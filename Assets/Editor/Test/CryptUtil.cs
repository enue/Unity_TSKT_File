﻿#nullable enable
using UnityEngine;
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
        public void Deflate(string data, string password, string salt, int iterations)
        {
            var bytesSalt = Encoding.UTF8.GetBytes(salt);
            var encData = TSKT.CryptUtil.Encrypt(CompressUtil.Compress(Encoding.UTF8.GetBytes(data)), password, bytesSalt, iterations);
            var decData = CompressUtil.Decompress(TSKT.CryptUtil.Decrypt(encData, password, bytesSalt, iterations));
            Assert.AreEqual(data, decData);
        }
        [Test]
        [TestCase("{\"guid\":\"d1dbd0bd-11c5-4067-aebf-319ca7da2808\", \"SoundManager.volume\":0, \"MusicManager.volume\":0}", "hogehogefugafuga", "piyopiyohogehoge", 1000)]
        [TestCase("{\"guid\":\"7454adca-97a3-4506-87f8-cda3151d0273\", \"SoundManager.volume\":-42.9644127,\"MusicManager.volume\":0}", "hkr8gar4gpaerg]a", "rp5y9-0z,z,32y^1", 100)]
        public void Brotli(string data, string password, string salt, int iterations)
        {
            var originalBytes = Encoding.UTF8.GetBytes(data);
            var bytesSalt = Encoding.UTF8.GetBytes(salt);
            var encData = TSKT.CryptUtil.Encrypt(CompressUtil.CompressByBrotli(new ReadOnlySequence<byte>(originalBytes)).ToArray(), password, bytesSalt, iterations);
            var decData = CompressUtil.DecompressByBrotli(new ReadOnlySequence<byte>(TSKT.CryptUtil.Decrypt(encData, password, bytesSalt, iterations)));
            Assert.AreEqual(data, Encoding.UTF8.GetString(decData));
        }

        [Test]
        [TestCase("{\"guid\":\"d1dbd0bd-11c5-4067-aebf-319ca7da2808\", \"SoundManager.volume\":0, \"MusicManager.volume\":0}", "hogehogefugafuga", "piyopiyohogehoge", 1000)]
        [TestCase("{\"guid\":\"7454adca-97a3-4506-87f8-cda3151d0273\", \"SoundManager.volume\":-42.9644127,\"MusicManager.volume\":0}", "hkr8gar4gpaerg]a", "rp5y9-0z,z,32y^1", 100)]
        public void Brotli2(string data, string password, string salt, int iterations)
        {
            var originalBytes = Encoding.UTF8.GetBytes(data);
            var bytesSalt = Encoding.UTF8.GetBytes(salt);
            var encData = TSKT.CryptUtil.Encrypt(CompressUtil.CompressByBrotli(originalBytes).ToArray(), password, bytesSalt, iterations);
            var decData = CompressUtil.DecompressByBrotli(TSKT.CryptUtil.Decrypt(encData, password, bytesSalt, iterations));
            Assert.AreEqual(data, Encoding.UTF8.GetString(decData));
        }
    }
}

