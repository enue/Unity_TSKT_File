#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Buffers;
using System.Security.Cryptography;

namespace TSKT.Files
{
    public interface ISerializeResolver
    {
        void Serialize<T>(T obj, IBufferWriter<byte> writer);
        Awaitable SerializeAsync<T>(T obj, IBufferWriter<byte> writer);
        T Deserialize<T>(ReadOnlySpan<byte> bytes);
        Awaitable<T> DeserializeAsync<T>(byte[] bytes);
    }

    public class JsonResolver : ISerializeResolver
    {
        readonly string? password;
        readonly byte[]? salt;
        readonly int iterations;
        readonly bool compress;
        public bool ShouldCrypt => password != null;

        // 鍵導出は重いので一度だけ行う。同じ値になるのでスレッド間で競合しても問題ない。
        byte[]? key;
        byte[] Key => key ??= CryptUtil.GenerateKey(password!, salt!, iterations);

        public JsonResolver()
        {
        }

        public JsonResolver(string password, byte[] salt, int iterations)
        {
            this.password = password;
            this.salt = salt;
            this.iterations = iterations;
            compress = ShouldCrypt;
        }

        public JsonResolver(bool compress)
        {
            this.compress = compress;
        }

        public void Serialize<T>(T obj, IBufferWriter<byte> writer)
        {
            var json = JsonUtility.ToJson(obj, prettyPrint: !compress && !ShouldCrypt);

            // セーブデータは大きくなりうるのでstackallocは使わない（スタックオーバーフローする）
            var encoding = System.Text.Encoding.UTF8;
            var rented = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(json));
            try
            {
                var jsonLength = encoding.GetBytes(json, rented);

                if (compress)
                {
                    WriteCompressed(rented.AsSpan(0, jsonLength), writer);
                }
                else
                {
                    Write(rented, jsonLength, writer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        const int HashSize = 256 / 8;

        /// <summary>
        /// 「ハッシュ + 圧縮データ」の形にして書き出す。コピーを減らすため、一つのバッファにハッシュの分を空けて直接圧縮する。
        /// </summary>
        void WriteCompressed(ReadOnlySpan<byte> jsonBytes, IBufferWriter<byte> writer)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(HashSize + System.IO.Compression.BrotliEncoder.GetMaxCompressedLength(jsonBytes.Length));
            try
            {
                var bodyLength = CompressUtil.CompressByBrotli(jsonBytes, buffer.AsSpan(HashSize));

                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    if (!sha.TryComputeHash(buffer.AsSpan(HashSize, bodyLength), buffer.AsSpan(0, HashSize), out var written)
                        || written != HashSize)
                    {
                        throw new Exception("failed to compute hash");
                    }
                }

                Write(buffer, HashSize + bodyLength, writer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// bufferの先頭からlengthバイトを、必要なら暗号化して書き出す。
        /// </summary>
        void Write(byte[] buffer, int length, IBufferWriter<byte> writer)
        {
            if (ShouldCrypt)
            {
                CryptUtil.Encrypt(buffer, 0, length, Key, writer);
            }
            else
            {
                writer.Write(buffer.AsSpan(0, length));
            }
        }

        public async Awaitable SerializeAsync<T>(T obj, IBufferWriter<byte> writer)
        {
#if UNITY_WEBGL
            Serialize(obj, writer);
#else
            await Awaitable.BackgroundThreadAsync();
            try
            {
                Serialize(obj, writer);
            }
            finally
            {
                // 例外時も呼び出し元をメインスレッドに戻す
                await Awaitable.MainThreadAsync();
            }
#endif
        }

        public T Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            try
            {
                ReadOnlySpan<byte> buffer;
                if (ShouldCrypt)
                {
                    buffer = CryptUtil.Decrypt(bytes, Key);
                }
                else
                {
                    buffer = bytes;
                }

                string jsonString;
                if (compress)
                {
                    //using var json = ZString.CreateUtf8StringBuilder();を使いたいがうまくいかないのでArrayBufferWriter
                    var json = new ArrayBufferWriter<byte>();
                    var hashSize = 256 / 8;
                    var signature = buffer[..hashSize];
                    var body = buffer[hashSize..];

                    // decompressする前にハッシュチェックを行う。というのもbrotliに雑なデータを食わせるとクラッシュする。
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    {
                        Span<byte> hashData = stackalloc byte[hashSize];
                        if (!sha.TryComputeHash(body, hashData, out var written))
                        {
                            throw new Exception();
                        }
                        if (!signature.SequenceEqual(hashData[..written]))
                        {
                            throw new Exception();
                        }
                    }
                    CompressUtil.DecompressByBrotli(body, json);
                    jsonString = System.Text.Encoding.UTF8.GetString(json.WrittenSpan);
                }
                else
                {
                    jsonString = System.Text.Encoding.UTF8.GetString(buffer);
                }

                return FromJson<T>(jsonString);
            }
            catch (Exception ex)
            {
                try
                {
                    var buffer = bytes;
                    if (ShouldCrypt)
                    {
                        // CryptUtil.Decryptでコケたときに旧形式で復号を試みる
                        buffer = CryptUtil.DecryptByCommonIV(buffer, password!, salt!, iterations);
                    }
                    if (compress)
                    {
                        buffer = CompressUtil.Decompress(buffer.ToArray());
                    }
                    var json = System.Text.Encoding.UTF8.GetString(buffer);
                    return FromJson<T>(json);
                }
                catch (Exception legacyException)
                {
                    // 旧形式でも読めなかった。現行形式で失敗した理由を先頭にして両方残す。
                    throw new AggregateException("failed to deserialize as current format and as legacy format", ex, legacyException);
                }
            }
        }

        static T FromJson<T>(string json)
        {
            // 空ファイルなどではFromJsonがnullを返す。成功扱いにすると呼び出し側にnullが渡るので失敗にする。
            var result = JsonUtility.FromJson<T>(json);
            if (result == null)
            {
                throw new Exception("empty json");
            }
            return result;
        }

        public async Awaitable<T> DeserializeAsync<T>(byte[] bytes)
        {
#if UNITY_WEBGL
            return Deserialize<T>(bytes);
#else
            await Awaitable.BackgroundThreadAsync();
            try
            {
                return Deserialize<T>(bytes);
            }
            finally
            {
                // 例外時も呼び出し元をメインスレッドに戻す
                await Awaitable.MainThreadAsync();
            }
#endif
        }

    }
}