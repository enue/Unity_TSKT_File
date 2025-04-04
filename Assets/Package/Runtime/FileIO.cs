#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Buffers;

namespace TSKT
{
    public class FileIO
    {
        readonly Dictionary<string, LoadResult<byte[]>> cache = new();
        public Files.ILoadSaveResolver Resolver { get; }
        public Files.ISerializeResolver SerializeResolver { get; }

        AwaitableCompletionSource? writingCompletion;

        public FileIO(Files.ILoadSaveResolver resolver, Files.ISerializeResolver serializeResolver)
        {
            Resolver = resolver;
            SerializeResolver = serializeResolver;
        }

        public ReadOnlySpan<byte> Save<T>(string filename, T obj)
        {
            var writer = new ArrayBufferWriter<byte>();
            SerializeResolver.Serialize(obj, writer);
            var result = writer.WrittenSpan;
            SaveBytes(filename, result);
            return result;
        }

        public void SaveBytes(string filename, ReadOnlySpan<byte> bytes)
        {
            if (cache.TryGetValue(filename, out var previous))
            {
                if (previous.Succeeded)
                {
                    if (bytes.SequenceEqual(previous.value))
                    {
                        return;
                    }
                }
            }
            Resolver.SaveBytes(filename, bytes);
            cache[filename] = new(bytes.ToArray());
        }

        /// <summary>
        /// シリアライズからファイルアクセスまですべて非同期で行う。途中でタスクキルされるとファイルが壊れることがあるので注意。対応済みの場合のみ採用すること
        /// </summary>
        public async Awaitable<byte[]> SaveAsync<T>(string filename, T obj, System.IProgress<float>? progress = null)
        {
            var myCompletion = new AwaitableCompletionSource();
            var previousCompletion = writingCompletion;
            writingCompletion = myCompletion;

            try
            {
                var writer = new ArrayBufferWriter<byte>();
                await SerializeResolver.SerializeAsync(obj, writer);
                progress?.Report(0.5f);

                if (previousCompletion != null)
                {
                    await previousCompletion.Awaitable;
                }

                var bytes = writer.WrittenSpan.ToArray();
                await SaveBytesAsyncInternal(filename, bytes);
                return bytes;
            }
            finally
            {
                progress?.Report(1f);

                myCompletion.TrySetResult();
                if (writingCompletion == myCompletion)
                {
                    writingCompletion = null;
                }
            }
        }

        public async Awaitable SaveBytesAsync(string filename, byte[] bytes, System.IProgress<float>? progress = null)
        {
            var myCompletion = new AwaitableCompletionSource();
            var previousCompletion = writingCompletion;
            writingCompletion = myCompletion;

            try
            {
                if (previousCompletion != null)
                {
                    await previousCompletion.Awaitable;
                }

                await SaveBytesAsyncInternal(filename, bytes);
            }
            finally
            {
                progress?.Report(1f);

                myCompletion.TrySetResult();
                if (writingCompletion == myCompletion)
                {
                    writingCompletion = null;
                }
            }
        }

        async Awaitable SaveBytesAsyncInternal(string filename, byte[] bytes)
        {
            if (cache.TryGetValue(filename, out var previous))
            {
                if (previous.Succeeded)
                {
                    if (previous.value.SequenceEqual(bytes))
                    {
                        return;
                    }
                }
            }
            using (new PreventFromQuitting(null))
            {
                await Resolver.SaveBytesAsync(filename, bytes);
            }
            cache[filename] = new(bytes);
        }

        public bool AnyExist(params string[] filenames)
        {
            if (filenames == null)
            {
                return false;
            }
            if (filenames.Length == 0)
            {
                return false;
            }
            foreach (var it in filenames)
            {
                if (cache.TryGetValue(it, out var result))
                {
                    if (result.Succeeded)
                    {
                        return true;
                    }
                }
            }
            return Resolver.AnyExist(filenames);
        }

        public async Awaitable<LoadResult<T>> LoadAsync<T>(string filename, System.IProgress<float>? progress = null)
        {
            try
            {
                if (!cache.TryGetValue(filename, out var bytes))
                {
                    try
                    {
                        bytes = await Resolver.LoadBytesAsync(filename);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                        return LoadResult<T>.CreateError(ex);
                    }

                    // LoadResult.State.Errorの場合はキャッシュしない。IOエラーなら再試行して成功するかもしれないからだ。
                    if (bytes.state is LoadResult.State.Succeeded or LoadResult.State.NotFound)
                    {
                        cache[filename] = bytes;
                    }
                }
                if (!bytes.Succeeded)
                {
                    return bytes.CreateFailed<T>();
                }

                progress?.Report(0.5f);

                try
                {
                    var t = await SerializeResolver.DeserializeAsync<T>(bytes.value);
                    return new LoadResult<T>(t);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(filename + " broken");
                    Debug.LogException(ex);
                    return LoadResult<T>.CreateFailedDeserialize(ex);
                }
            }
            finally
            {
                progress?.Report(1f);
            }
        }

        public LoadResult<T> Load<T>(string filename)
        {
            if (!cache.TryGetValue(filename, out var bytes))
            {
                try
                {
                    bytes = Resolver.LoadBytes(filename);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return LoadResult<T>.CreateError(ex);
                }

                // LoadResult.State.Errorの場合はキャッシュしない。IOエラーなら再試行して成功するかもしれないからだ。
                if (bytes.state is LoadResult.State.Succeeded or LoadResult.State.NotFound)
                {
                    cache[filename] = bytes;
                }
            }
            if (!bytes.Succeeded)
            {
                return bytes.CreateFailed<T>();
            }

            try
            {
                var t = SerializeResolver.Deserialize<T>(bytes.value);
                return new LoadResult<T>(t);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(filename + " broken");
                Debug.LogException(ex);
                return LoadResult<T>.CreateFailedDeserialize(ex);
            }
        }
    }
}