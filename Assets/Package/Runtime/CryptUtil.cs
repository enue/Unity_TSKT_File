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
        public static void GenerateKeyAndIV(out byte[] key, out byte[] iv, string password, byte[] salt, int iterations, int keySize, int blockSize)
        {
            // パスワードから共有キーと初期化ベクターを作成
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations: iterations);
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }
        const int KeySize = 128;
        const int BlockSize = 128;

        /// <summary>
        /// Encrypt / Decryptで使う鍵を作る。鍵導出は重いので、同じパスワードで何度も暗号化するなら結果を使い回すこと。
        /// </summary>
        public static byte[] GenerateKey(string password, byte[] salt, int iterations)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations: iterations);
            return deriveBytes.GetBytes(KeySize / 8);
        }

        public static Aes CreateAes(byte[] key)
        {
            var result = Aes.Create();

            result.KeySize = KeySize;
            result.BlockSize = BlockSize;
            result.Key = key;
            return result;
        }

        public static byte[] Encrypt(ReadOnlySpan<byte> bytes, string password, byte[] salt, int iterations)
        {
            var writer = new ArrayBufferWriter<byte>();
            Encrypt(bytes.ToArray(), 0, bytes.Length, GenerateKey(password, salt, iterations), writer);
            return writer.WrittenSpan.ToArray();
        }

        /// <summary>
        /// 配列の一部をコピーなしで暗号化する。呼び出し側がすでに配列を持っているとき用。
        /// </summary>
        internal static void Encrypt(byte[] buffer, int offset, int count, byte[] key, IBufferWriter<byte> writer)
        {
            using var aes = CreateAes(key);
            using var encryptor = aes.CreateEncryptor();
            var iv = aes.IV;
            var body = encryptor.TransformFinalBlock(buffer, offset, count);
            writer.Write(iv);
            writer.Write(body);
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> encryptedBytes, string key, byte[] salt, int iterations)
        {
            return Decrypt(encryptedBytes, GenerateKey(key, salt, iterations));
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> encryptedBytes, byte[] key)
        {
            using var aes = CreateAes(key);
            var iv = encryptedBytes.Slice(0, aes.BlockSize / 8).ToArray();
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedBytes.ToArray(), iv.Length, encryptedBytes.Length - iv.Length);
        }

        [System.Obsolete]
        public static byte[] DecryptByCommonIV(ReadOnlySpan<byte> encryptedBytes, string key, byte[] salt, int iterations)
        {
            GenerateKeyAndIV(out var keyBytes, out var iv, key, salt, iterations, KeySize, BlockSize);
            using var aes = CreateAes(keyBytes);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedBytes.ToArray(), 0, encryptedBytes.Length);
        }
    }
}
