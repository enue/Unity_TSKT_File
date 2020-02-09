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
            var compressedBytes = Compress(bytes);
            return encryptor.TransformFinalBlock(compressedBytes, 0, compressedBytes.Length);
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
            var compressedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Decompress(compressedBytes);
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

        static public byte[] Compress(byte[] bytes)
        {
            using (var compressed = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(compressed, CompressionMode.Compress))
                {
                    deflateStream.Write(bytes, 0, bytes.Length);
                }

                return compressed.ToArray();
            }
        }

        static public byte[] Decompress(byte[] bytes)
        {
            using (var compressed = new MemoryStream(bytes))
            {
                using (var deflateStream = new DeflateStream(compressed, CompressionMode.Decompress))
                {
                    using (var decompressed = new MemoryStream())
                    {
                        deflateStream.CopyTo(decompressed);
                        return decompressed.ToArray();
                    }
                }
            }
        }
    }
}