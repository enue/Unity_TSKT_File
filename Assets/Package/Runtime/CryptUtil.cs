#nullable enable
using UnityEngine;
using System.Collections;
using System;
using System.Security.Cryptography;
using System.Buffers;

namespace TSKT
{
    public static class CryptUtil
    {
        public static void GenerateKey(out byte[] key, string password, byte[] salt, int iterations, int keySize)
        {
            // パスワードから共有キーを作成
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations: iterations);
            key = deriveBytes.GetBytes(keySize / 8);
        }
        public static void GenerateKeyAndIV(out byte[] key, out byte[] iv, string password, byte[] salt, int iterations, int keySize, int blockSize)
        {
            // パスワードから共有キーと初期化ベクターを作成
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations: iterations);
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }
        public static Aes CreateAes(string password, byte[] salt, int iterations)
        {
            var result = Aes.Create();

            result.KeySize = 128;
            result.BlockSize = 128;
            GenerateKey(out var key, password, salt, iterations, result.KeySize);
            result.Key = key;
            return result;
        }

        public static ReadOnlyMemory<byte> Encrypt(ReadOnlySpan<byte> bytes, string password, byte[] salt, int iterations)
        {
            using (var aes = CreateAes(password, salt, iterations))
            {
                using (var encryptor = aes.CreateEncryptor())
                {
                    var iv = aes.IV;
                    var body = encryptor.TransformFinalBlock(bytes.ToArray(), 0, bytes.Length);
                    var writer = new ArrayBufferWriter<byte>(iv.Length + body.Length);
                    writer.Write(iv);
                    writer.Write(body);
                    return writer.WrittenMemory;
                }
            }
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> encryptedBytes, string key, byte[] salt, int iterations)
        {
            using (var aes = CreateAes(key, salt, iterations))
            {
                var iv = encryptedBytes.Slice(0, aes.BlockSize / 8).ToArray();
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptedBytes.ToArray(), iv.Length, encryptedBytes.Length - iv.Length);
                }
            }
        }

        [System.Obsolete]
        public static byte[] DecryptByCommonIV(ReadOnlySpan<byte> encryptedBytes, string key, byte[] salt, int iterations)
        {
            using (var aes = CreateAes(key, salt, iterations))
            {
                GenerateKeyAndIV(out _, out var iv, key, salt, iterations, aes.KeySize, aes.BlockSize);
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptedBytes.ToArray(), 0, encryptedBytes.Length);
                }
            }
        }
    }
}
