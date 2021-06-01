using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT.Files
{
    public interface ISerializeResolver
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] bytes);
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
                bytes = CompressUtil.Compress(bytes);
            }
            if (ShouldCrypt)
            {
                bytes = CryptUtil.Encrypt(bytes, password, salt, iterations);
            }
            return bytes;
        }

        public T Deserialize<T>(byte[] bytes)
        {
            if (ShouldCrypt)
            {
                bytes = CryptUtil.Decrypt(bytes, password, salt, iterations);
            }
            if (compress)
            {
                bytes = CompressUtil.Decompress(bytes);
            }
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<T>(json);
        }
    }
}