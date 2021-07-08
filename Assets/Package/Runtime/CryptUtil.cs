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
        static public RijndaelManaged CreateRijndael(string password, string salt, int iterations)
        {
            return CreateRijndael(password, Encoding.UTF8.GetBytes(salt), iterations);
        }

        static public RijndaelManaged CreateRijndael(string password, byte[] salt, int iterations)
        {
            var rijndael = new RijndaelManaged
            {
                KeySize = 128,
                BlockSize = 128
            };

            // パスワードから共有キーと初期化ベクターを作成
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations: iterations))
            {
                rijndael.Key = deriveBytes.GetBytes(rijndael.KeySize / 8);
                rijndael.IV = deriveBytes.GetBytes(rijndael.BlockSize / 8);
            }

            return rijndael;
        }

        static public byte[] Encrypt(byte[] bytes, ICryptoTransform encryptor)
        {
            return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        static public byte[] Encrypt(byte[] bytes, string password, byte[] salt, int iterations)
        {
            using (var rijndael = CreateRijndael(password, salt, iterations))
            {
                using (var encryptor = rijndael.CreateEncryptor())
                {
                    return Encrypt(bytes, encryptor);
                }
            }
        }

        static public byte[] Decrypt(byte[] encryptedBytes, ICryptoTransform decryptor)
        {
            return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        }

        static public byte[] Decrypt(byte[] encryptedBytes, string key, byte[] salt, int iterations)
        {
            using (var rijndael = CreateRijndael(key, salt, iterations))
            {
                using (var decryptor = rijndael.CreateDecryptor())
                {
                    return Decrypt(encryptedBytes, decryptor);
                }
            }
        }
    }
}