#nullable enable
using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

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

        public static byte[] Encrypt(byte[] bytes, string password, byte[] salt, int iterations)
        {
            using (var aes = CreateAes(password, salt, iterations))
            {
                using (var encryptor = aes.CreateEncryptor())
                {
                    var iv = aes.IV;
                    var body = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                    var result = new byte[iv.Length + body.Length];
                    Array.Copy(iv, result, iv.Length);
                    Array.Copy(body, 0, result, iv.Length, body.Length);
                    return result;
                }
            }
        }

        public static byte[] Decrypt(byte[] encryptedBytes, string key, byte[] salt, int iterations)
        {
            using (var aes = CreateAes(key, salt, iterations))
            {
                try
                {
                    var iv = encryptedBytes.AsSpan(0, aes.BlockSize / 8).ToArray();
                    aes.IV = iv;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
                    }
                }
                catch (CryptographicException)
                {
                    GenerateKeyAndIV(out _, out var iv, key, salt, iterations, aes.KeySize, aes.BlockSize);
                    aes.IV = iv;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    }
                }
            }
        }
    }
}
