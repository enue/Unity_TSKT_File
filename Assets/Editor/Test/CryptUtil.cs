#nullable enable
using UnityEngine;
using System;
using System.Collections;
using NUnit.Framework;
using System.Text;
using System.Buffers;

namespace TSKT.Tests
{
    public class CryptUtil
    {
        [Test]
        [TestCase("{\"guid\":\"d1dbd0bd-11c5-4067-aebf-319ca7da2808\", \"SoundManager.volume\":0, \"MusicManager.volume\":0}", "hogehogefugafuga", "piyopiyohogehoge", 1000)]
        [TestCase("{\"guid\":\"7454adca-97a3-4506-87f8-cda3151d0273\", \"SoundManager.volume\":-42.9644127,\"MusicManager.volume\":0}", "hkr8gar4gpaerg]a", "rp5y9-0z,z,32y^1", 100)]
        public void Brotli(string data, string password, string salt, int iterations)
        {
            var originalBytes = Encoding.UTF8.GetBytes(data);
            var bytesSalt = Encoding.UTF8.GetBytes(salt);
            var compressed = new byte[System.IO.Compression.BrotliEncoder.GetMaxCompressedLength(originalBytes.Length)];
            var compressedLength = CompressUtil.CompressByBrotli(originalBytes, compressed);
            var encData = TSKT.CryptUtil.Encrypt(compressed.AsSpan(0, compressedLength), TSKT.CryptUtil.GenerateKey(password, bytesSalt, iterations));
            var decompressed = new ArrayBufferWriter<byte>();
            CompressUtil.DecompressByBrotli(TSKT.CryptUtil.Decrypt(encData, TSKT.CryptUtil.GenerateKey(password, bytesSalt, iterations)), decompressed);
            Assert.AreEqual(data, Encoding.UTF8.GetString(decompressed.WrittenSpan));
        }
    }
}

