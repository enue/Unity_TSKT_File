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
        public static byte[] Compress(ReadOnlySpan<byte> bytes)
        {
            using (var compressed = new MemoryStream())
            {
                using (var stream = new BrotliStream(compressed, CompressionMode.Compress))
                {
                    stream.Write(bytes);
                }
                return compressed.ToArray();
            }
        }

        public static byte[] Decompress(byte[] bytes)
        {
            try
            {
                using (var compressed = new MemoryStream(bytes))
                {
                    using (var stream = new BrotliStream(compressed, CompressionMode.Decompress))
                    {
                        var decompressed = new MemoryStream();
                        stream.CopyTo(decompressed);
                        return decompressed.ToArray();
                    }
                }
            }
            catch
            {
                using (var compressed = new MemoryStream(bytes))
                {
                    using (var deflateStream = new DeflateStream(compressed, CompressionMode.Decompress))
                    {
                        var decompressed = new MemoryStream();
                        deflateStream.CopyTo(decompressed);
                        return decompressed.ToArray();
                    }
                }
            }
        }
    }
}
