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
            Span<byte> jsonBytes = stackalloc byte[System.Text.Encoding.UTF8.GetMaxByteCount(json.Length)];
            {
                var l = System.Text.Encoding.UTF8.GetBytes(json, jsonBytes);
                jsonBytes = jsonBytes[..l];
            }

            if (compress)
            {
                ReadOnlySpan<byte> body;
                {
                    var length = System.IO.Compression.BrotliEncoder.GetMaxCompressedLength(jsonBytes.Length);
                    var compressed = new ArrayBufferWriter<byte>(length);
                    CompressUtil.CompressByBrotli(jsonBytes, compressed);
                    body = compressed.WrittenSpan;
                }

                using var sha = new System.Security.Cryptography.SHA256Managed();
                var hashSize = 256 / 8;
                Span<byte> bytes = stackalloc byte[body.Length + hashSize];
                if (!sha.TryComputeHash(body, bytes, out var written))
                {
                    throw new Exception();
                }
                body.CopyTo(bytes[written..]);
                bytes = bytes[..(body.Length + written)];

                if (ShouldCrypt)
                {
                    CryptUtil.Encrypt(bytes, password!, salt!, iterations, writer);
                }
                else
                {
                    writer.Write(bytes);
                }
            }
            else
            {
                if (ShouldCrypt)
                {
                    CryptUtil.Encrypt(jsonBytes, password!, salt!, iterations, writer);
                }
                else
                {
                    writer.Write(jsonBytes);
                }
            }
        }

        public async Awaitable SerializeAsync<T>(T obj, IBufferWriter<byte> writer)
        {
#if UNITY_WEBGL
            return Serialize(obj).ToArray();
#else
            await Awaitable.BackgroundThreadAsync();
            Serialize(obj, writer);
            await Awaitable.MainThreadAsync();
#endif
        }

        public T Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            try
            {
                var buffer = bytes;
                if (ShouldCrypt)
                {
                    buffer = CryptUtil.Decrypt(buffer, password!, salt!, iterations);
                }

                if (compress)
                {
                    // decompressする前にハッシュチェックを行う。というのもbrotliに雑なデータを食わせるとクラッシュする。
                    using (var sha = new System.Security.Cryptography.SHA256Managed())
                    {
                        var hashSize = 256 / 8;
                        var signature = buffer[..hashSize];
                        buffer = buffer[hashSize..];
                        var hashData = new ArrayBufferWriter<byte>(hashSize);
                        if (!sha.TryComputeHash(buffer, hashData.GetSpan(hashSize), out var written))
                        {
                            throw new Exception();
                        }
                        hashData.Advance(written);
                        if (!signature.SequenceEqual(hashData.WrittenSpan))
                        {
                            throw new Exception();
                        }
                    }

                    var writer = new ArrayBufferWriter<byte>();
                    CompressUtil.DecompressByBrotli(buffer, writer);
                    buffer = writer.WrittenSpan;
                }

                var json = System.Text.Encoding.UTF8.GetString(buffer);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
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
                return JsonUtility.FromJson<T>(json);
            }
        }

        public async Awaitable<T> DeserializeAsync<T>(byte[] bytes)
        {
#if UNITY_WEBGL
            return Deserialize<T>(bytes);
#else
            await Awaitable.BackgroundThreadAsync();
            var result = Deserialize<T>(bytes);
            await Awaitable.MainThreadAsync();
            return result;
#endif
        }

    }
}