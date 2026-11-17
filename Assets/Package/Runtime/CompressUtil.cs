#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Buffers;

namespace TSKT
{
    public static class CompressUtil
    {
        const int BrotliQuality = 4;
        const int BrotliWindow = 24;

        // IBufferWriterに要求する最小の空き。指定しないと数バイトの領域が返ってループ回数が増える。
        const int BufferSizeHint = 16 * 1024;

        /// <summary>
        /// destinationに圧縮して、書き込んだバイト数を返す。destinationはBrotliEncoder.GetMaxCompressedLength(source.Length)以上の長さが必要。
        /// </summary>
        public static int CompressByBrotli(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!BrotliEncoder.TryCompress(source, destination, out var written, BrotliQuality, BrotliWindow))
            {
                throw new Exception("failed to compress");
            }
            return written;
        }

        public static void DecompressByBrotli(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
        {
            using var decoder = new BrotliDecoder();

            for (; ; )
            {
                var status = decoder.Decompress(source, writer.GetSpan(BufferSizeHint), out var consumed, out var written);
                if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

                source = source[consumed..];
                writer.Advance(written);
                if (status == OperationStatus.Done) break;
                // 入力は全部渡しているので、足りないなら途中で切れたデータ
                if (status == OperationStatus.NeedMoreData) throw new Exception("truncated data");
            }
        }
    }
}
