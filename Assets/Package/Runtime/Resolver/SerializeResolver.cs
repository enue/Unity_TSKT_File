#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;

namespace TSKT.Files
{
    public interface ISerializeResolver
    {
        byte[] Serialize<T>(T obj);
        UniTask<byte[]> SerializeAsync<T>(T obj);
        T Deserialize<T>(byte[] bytes);
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

        public byte[] Serialize<T>(T obj)
        {
            var json = JsonUtility.ToJson(obj, prettyPrint: !compress && !ShouldCrypt);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            if (compress)
            {
                bytes = CompressUtil.CompressByBrotli(bytes);

                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    var hashData = sha.ComputeHash(bytes);
                    var combined = new byte[bytes.Length + hashData.Length];
                    Array.Copy(hashData, combined, hashData.Length);
                    Array.Copy(bytes, 0, combined, hashData.Length, bytes.Length);
                    bytes = combined;
                }
            }

            if (ShouldCrypt)
            {
                bytes = CryptUtil.Encrypt(bytes, password!, salt!, iterations);
            }
            return bytes;
        }

        public UniTask<byte[]> SerializeAsync<T>(T obj)
        {
#if UNITY_WEBGL
            return UniTask.FromResult(Serialize(obj));
#else
            return UniTask.RunOnThreadPool(() => Serialize(obj));
#endif
        }

        public T Deserialize<T>(byte[] bytes)
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
                    var signature = buffer.AsSpan(0, 256 / 8);
                    buffer = buffer.AsSpan(256 / 8).ToArray();
                    using (var sha = new System.Security.Cryptography.SHA256Managed())
                    {
                        var hashData = sha.ComputeHash(buffer);
                        if (!signature.SequenceEqual(hashData))
                        {
                            throw new Exception();
                        }
                    }

                    buffer = CompressUtil.DecompressByBrotli(buffer);
                }

                var json = System.Text.Encoding.UTF8.GetString(buffer);
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                var buffer = bytes;
                if (ShouldCrypt)
                {
                    buffer = CryptUtil.DecryptByCommonIV(buffer, password!, salt!, iterations);
                }
                if (compress)
                {
                    buffer = CompressUtil.Decompress(buffer);
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