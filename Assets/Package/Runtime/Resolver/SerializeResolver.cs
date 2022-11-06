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
        ReadOnlyMemory<byte> Serialize<T>(T obj);
        UniTask<ReadOnlyMemory<byte>> SerializeAsync<T>(T obj);
        T Deserialize<T>(ReadOnlyMemory<byte> bytes);
        UniTask<T> DeserializeAsync<T>(ReadOnlyMemory<byte> bytes);
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

        public ReadOnlyMemory<byte> Serialize<T>(T obj)
        {
            var json = JsonUtility.ToJson(obj, prettyPrint: !compress && !ShouldCrypt);
            var bytes = new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes(json));

            if (compress)
            {
                bytes = CompressUtil.CompressByBrotli(new ReadOnlySequence<byte>(bytes));

                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    var hashSize = 256 / 8;
                    var writer = new ArrayBufferWriter<byte>(bytes.Length + hashSize);
                    if (!sha.TryComputeHash(bytes.Span, writer.GetSpan(hashSize), out var written))
                    {
                        throw new Exception();
                    }
                    writer.Advance(written);
                    writer.Write(bytes.Span);
                    bytes = writer.WrittenMemory;
                }
            }

            if (ShouldCrypt)
            {
                bytes = CryptUtil.Encrypt(bytes, password!, salt!, iterations);
            }
            return bytes;
        }

        public UniTask<ReadOnlyMemory<byte>> SerializeAsync<T>(T obj)
        {
#if UNITY_WEBGL
            return UniTask.FromResult(Serialize(obj));
#else
            return UniTask.RunOnThreadPool(() => Serialize(obj));
#endif
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> bytes)
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
                        var hashData = new ArrayBufferWriter<byte>();
                        if (!sha.TryComputeHash(buffer.Span, hashData.GetSpan(hashSize), out var written))
                        {
                            throw new Exception();
                        }
                        hashData.Advance(written);
                        if (!signature.Span.SequenceEqual(hashData.WrittenSpan))
                        {
                            throw new Exception();
                        }
                    }

                    buffer = CompressUtil.DecompressByBrotli(new ReadOnlySequence<byte>(buffer));
                }

                var json = System.Text.Encoding.UTF8.GetString(buffer.Span);
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
                var json = System.Text.Encoding.UTF8.GetString(buffer.Span);
                return JsonUtility.FromJson<T>(json);
            }
        }

        public UniTask<T> DeserializeAsync<T>(ReadOnlyMemory<byte> bytes)
        {
#if UNITY_WEBGL
            return UniTask.FromResult(Deserialize<T>(bytes));
#else
            return UniTask.RunOnThreadPool(() => Deserialize<T>(bytes));
#endif
        }

    }
}