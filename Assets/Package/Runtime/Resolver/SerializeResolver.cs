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
        readonly byte[]? key;

        public JsonResolver()
        {
        }

        public JsonResolver(string password, byte[] salt, int iterations)
        {
            key = CryptUtil.GenerateKey(password, salt, iterations);
        }


        public void Serialize<T>(T obj, IBufferWriter<byte> writer)
        {
            var json = JsonUtility.ToJson(obj, prettyPrint: key == null);

            // セーブデータは大きくなりうるのでstackallocは使わない（スタックオーバーフローする）
            var encoding = System.Text.Encoding.UTF8;
            var rented = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(json));
            try
            {
                var jsonLength = encoding.GetBytes(json, rented);

                if (key != null)
                {
                    WriteEncrypted(rented.AsSpan(0, jsonLength), key, writer);
                }
                else
                {
                    writer.Write(rented.AsSpan(0, jsonLength));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        const int HashSize = 256 / 8;

        /// <summary>
        /// 圧縮して「ハッシュ + 圧縮データ」の形にし、それを暗号化して書き出す。コピーを減らすため、一つのバッファにハッシュの分を空けて直接圧縮する。
        /// </summary>
        static void WriteEncrypted(ReadOnlySpan<byte> jsonBytes, byte[] key, IBufferWriter<byte> writer)
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

                CryptUtil.Encrypt(buffer, 0, HashSize + bodyLength, key, writer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
            string jsonString;

            if (key != null)
            {
                ReadOnlySpan<byte> buffer = CryptUtil.Decrypt(bytes, key);
                //using var json = ZString.CreateUtf8StringBuilder();を使いたいがうまくいかないのでArrayBufferWriter
                var json = new ArrayBufferWriter<byte>();
                var signature = buffer[..HashSize];
                var body = buffer[HashSize..];

                // decompressする前にハッシュチェックを行う。というのもbrotliに雑なデータを食わせるとクラッシュする。
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    Span<byte> hashData = stackalloc byte[HashSize];
                    if (!sha.TryComputeHash(body, hashData, out var written))
                    {
                        throw new Exception("failed to compute hash");
                    }
                    if (!signature.SequenceEqual(hashData[..written]))
                    {
                        throw new System.IO.InvalidDataException("hash mismatch. data is broken or not in the expected format");
                    }
                }
                CompressUtil.DecompressByBrotli(body, json);
                jsonString = System.Text.Encoding.UTF8.GetString(json.WrittenSpan);
            }
            else
            {
                jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            }

            return FromJson<T>(jsonString);
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