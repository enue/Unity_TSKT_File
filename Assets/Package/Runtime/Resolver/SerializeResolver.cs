#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Buffers;

namespace TSKT.Files
{
    public interface ISerializeResolver
    {
        ReadOnlySpan<byte> Serialize<T>(T obj);
        UniTask<byte[]> SerializeAsync<T>(T obj);
        T Deserialize<T>(ReadOnlySpan<byte> bytes);
        UniTask<T> DeserializeAsync<T>(byte[] bytes);
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

        public ReadOnlySpan<byte> Serialize<T>(T obj)
        {
            var json = JsonUtility.ToJson(obj, prettyPrint: !compress && !ShouldCrypt);
            ReadOnlySpan<byte> buffer;

            if (compress)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var body = CompressUtil.CompressByBrotli(bytes);

                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    var hashSize = 256 / 8;
                    var writer = new ArrayBufferWriter<byte>(body.Length + hashSize);
                    if (!sha.TryComputeHash(body, writer.GetSpan(hashSize), out var written))
                    {
                        throw new Exception();
                    }
                    writer.Advance(written);
                    writer.Write(body);
                    buffer = writer.WrittenSpan;
                }
            }
            else
            {
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }

            if (ShouldCrypt)
            {
                buffer = CryptUtil.Encrypt(buffer, password!, salt!, iterations);
            }
            return buffer;
        }

        public UniTask<byte[]> SerializeAsync<T>(T obj)
        {
#if UNITY_WEBGL
            return UniTask.FromResult(Serialize(obj).ToArray());
#else
            return UniTask.RunOnThreadPool(() => Serialize(obj).ToArray());
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

                    buffer = CompressUtil.DecompressByBrotli(buffer);
                }

                var json = System.Text.Encoding.UTF8.GetString(buffer);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                var buffer = bytes;
                if (ShouldCrypt)
                {
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

        public UniTask<T> DeserializeAsync<T>(byte[] bytes)
        {
#if UNITY_WEBGL
            return UniTask.FromResult(Deserialize<T>(bytes));
#else
            return UniTask.RunOnThreadPool(() => Deserialize<T>(bytes));
#endif
        }

    }
}