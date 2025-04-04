﻿#nullable enable
using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Buffers;

namespace TSKT
{
    public static class CompressUtil
    {
        public static ReadOnlySpan<byte> CompressByBrotli(ReadOnlySpan<byte> source)
        {
            var length = BrotliEncoder.GetMaxCompressedLength(source.Length);
            var writer = new ArrayBufferWriter<byte>(length);
            CompressByBrotli(source, writer);
            return writer.WrittenSpan;
        }

        public static void CompressByBrotli(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
        {
            using var encoder = new BrotliEncoder(11, 24);

            for (; ; )
            {
                var status = encoder.Compress(source, writer.GetSpan(), out var consumed, out var written, false);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                source = source[consumed..];
                writer.Advance(written);
                if (status == OperationStatus.Done) break;
            }

            for (; ; )
            {
                var status = encoder.Compress(ReadOnlySpan<byte>.Empty, writer.GetSpan(), out _, out var written, true);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                writer.Advance(written);
                if (written == 0) break;
            }
        }

        public static ReadOnlySpan<byte> CompressByBrotli(ReadOnlySequence<byte> sequence)
        {
            var length = BrotliEncoder.GetMaxCompressedLength((int)sequence.Length);
            var writer = new ArrayBufferWriter<byte>(length);
            CompressByBrotli(sequence, writer);
            return writer.WrittenSpan;
        }

        public static void CompressByBrotli(ReadOnlySequence<byte> sequence, IBufferWriter<byte> writer)
        {
            using var encoder = new BrotliEncoder(11, 24);

            var reader = new SequenceReader<byte>(sequence);

            for (; ; )
            {
                var status = encoder.Compress(reader.UnreadSpan, writer.GetSpan(), out var consumed, out var written, false);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                reader.Advance(consumed);
                writer.Advance(written);
                if (status == OperationStatus.Done) break;
            }

            for (; ; )
            {
                var status = encoder.Compress(ReadOnlySpan<byte>.Empty, writer.GetSpan(), out _, out var written, true);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                writer.Advance(written);
                if (written == 0) break;
            }
        }

        public static ReadOnlySpan<byte> DecompressByBrotli(ReadOnlySpan<byte> source)
        {
            var writer = new ArrayBufferWriter<byte>();
            DecompressByBrotli(source, writer);
            return writer.WrittenSpan;
        }
        public static void DecompressByBrotli(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
        {
            using var decoder = new BrotliDecoder();

            for (; ; )
            {
                var status = decoder.Decompress(source, writer.GetSpan(), out var consumed, out var written);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                source = source[consumed..];
                writer.Advance(written);
                if (written == 0) break;
            }
        }

        public static ReadOnlySpan<byte> DecompressByBrotli(ReadOnlySequence<byte> sequence)
        {
            var writer = new ArrayBufferWriter<byte>();
            DecompressByBrotli(sequence, writer);
            return writer.WrittenSpan;
        }
        public static void DecompressByBrotli(ReadOnlySequence<byte> sequence, IBufferWriter<byte> writer)
        {
            var reader = new SequenceReader<byte>(sequence);

            using var decoder = new BrotliDecoder();

            for (; ; )
            {
                var status = decoder.Decompress(reader.UnreadSpan, writer.GetSpan(), out var consumed, out var written);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                reader.Advance(consumed);
                writer.Advance(written);
                if (written == 0) break;
            }
        }

        public static byte[] Compress(ReadOnlySpan<byte> bytes)
        {
            using var compressed = new MemoryStream();
            using (var stream = new DeflateStream(compressed, CompressionMode.Compress))
            {
                stream.Write(bytes);
            }
            return compressed.ToArray();
        }
        public static byte[] Decompress(byte[] bytes)
        {
            using var compressed = new MemoryStream(bytes);
            using var deflateStream = new DeflateStream(compressed, CompressionMode.Decompress);
            using var decompressed = new MemoryStream();
            deflateStream.CopyTo(decompressed);
            return decompressed.ToArray();
        }
    }
}
