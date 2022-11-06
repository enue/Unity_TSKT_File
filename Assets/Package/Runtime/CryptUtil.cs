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

            result.KeySize = 256;
            result.BlockSize = 128;
            GenerateKeyAndIV(out var key, out var iv, password, salt, iterations, result.KeySize, result.BlockSize);
            result.Key = key;
            result.IV = iv;
            return result;
        }

        public static byte[] EncryptByAes(byte[] bytes, string password, byte[] salt, int iterations)
        {
            using (var rijndael = CreateAes(password, salt, iterations))
            {
                using (var encryptor = rijndael.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }
        }
        public static byte[] DecryptByAes(byte[] encryptedBytes, string key, byte[] salt, int iterations)
        {
            using (var rijndael = CreateAes(key, salt, iterations))
            {
                using (var decryptor = rijndael.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                }
            }
        }

        [System.Obsolete]
        public static RijndaelManaged CreateRijndael(string password, string salt, int iterations)
        {
            return CreateRijndael(password, Encoding.UTF8.GetBytes(salt), iterations);
        }

        [System.Obsolete]
        public static RijndaelManaged CreateRijndael(string password, byte[] salt, int iterations)
        {
            var rijndael = new RijndaelManaged
            {
                KeySize = 128,
                BlockSize = 128
            };
            GenerateKeyAndIV(out var key, out var iv, password, salt, iterations, rijndael.KeySize, rijndael.BlockSize);
            rijndael.Key = key;
            rijndael.IV = iv;

            return rijndael;
        }

        [System.Obsolete]
        public static byte[] Encrypt(byte[] bytes, string password, byte[] salt, int iterations)
        {
            using (var rijndael = CreateRijndael(password, salt, iterations))
            {
                using (var encryptor = rijndael.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }
        }


        [System.Obsolete]
        public static byte[] Decrypt(byte[] encryptedBytes, string key, byte[] salt, int iterations)
        {
            using (var rijndael = CreateRijndael(key, salt, iterations))
            {
                using (var decryptor = rijndael.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                }
            }
        }
    }
}