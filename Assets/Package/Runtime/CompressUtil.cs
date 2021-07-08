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
    public static class CompressUtil
    {
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

        static MemoryStream DecompressToStream(byte[] bytes)
        {
            using (var compressed = new MemoryStream(bytes))
            {
                using (var deflateStream = new DeflateStream(compressed, CompressionMode.Decompress))
                {
                    var decompressed = new MemoryStream();
                    deflateStream.CopyTo(decompressed);
                    return decompressed;
                }
            }
        }
        static public string DecompressToString(byte[] bytes)
        {
            using (var stream = DecompressToStream(bytes))
            {
                return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }
        }

        static public byte[] Decompress(byte[] bytes)
        {
            using (var stream = DecompressToStream(bytes))
            {
                return stream.ToArray();
            }
        }
    }
}