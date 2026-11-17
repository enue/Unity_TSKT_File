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

        // 最後に並んだ非同期セーブが終わると完了するタスク。次のセーブはこれを待ってから書き込む。
        // SemaphoreSlimは待ちの解除にスレッドプールを使うのでWebGLでは止まる。TaskCompletionSourceなら使わない。
        System.Threading.Tasks.Task saveTail = System.Threading.Tasks.Task.CompletedTask;

        public FileIO(Files.ILoadSaveResolver resolver, Files.ISerializeResolver serializeResolver)
        {
            Resolver = resolver;
            SerializeResolver = serializeResolver;
        }

        public byte[] Save<T>(string filename, T obj)
        {
            var writer = new ArrayBufferWriter<byte>();
            SerializeResolver.Serialize(obj, writer);
            var bytes = writer.WrittenSpan.ToArray();
            if (!IsCached(filename, bytes))
            {
                Resolver.SaveBytes(filename, bytes);
                cache[filename] = new(bytes);
            }
            return bytes;
        }

        public void SaveBytes(string filename, ReadOnlySpan<byte> bytes)
        {
            if (IsCached(filename, bytes))
            {
                return;
            }
            Resolver.SaveBytes(filename, bytes);
            cache[filename] = new(bytes.ToArray());
        }

        bool IsCached(string filename, ReadOnlySpan<byte> bytes)
        {
            return cache.TryGetValue(filename, out var previous)
                && previous.Succeeded
                && bytes.SequenceEqual(previous.value);
        }

        /// <summary>
        /// シリアライズからファイルアクセスまですべて非同期で行う。途中でタスクキルされるとファイルが壊れることがあるので注意。対応済みの場合のみ採用すること
        /// </summary>
        public async Awaitable<byte[]> SaveAsync<T>(string filename, T obj, System.IProgress<float>? progress = null)
        {
            // シリアライズより先に並んでおく。書き込みを呼び出し順にするため。
            var turn = EnqueueSave(out var done);

            try
            {
                byte[] bytes;
                try
                {
                    var writer = new ArrayBufferWriter<byte>();
                    await SerializeResolver.SerializeAsync(obj, writer);
                    bytes = writer.WrittenSpan.ToArray();

                    progress?.Report(0.5f);
                }
                finally
                {
                    // シリアライズに失敗しても順番は待つ。待たずに次へ番を渡すと先行のセーブと並走してしまう。
                    await turn;
                }

                await SaveBytesAsyncInternal(filename, bytes);
                return bytes;
            }
            finally
            {
                progress?.Report(1f);
                done.TrySetResult(true);
            }
        }

        public async Awaitable SaveBytesAsync(string filename, byte[] bytes, System.IProgress<float>? progress = null)
        {
            var turn = EnqueueSave(out var done);
            try
            {
                await turn;
                await SaveBytesAsyncInternal(filename, bytes);
            }
            finally
            {
                progress?.Report(1f);
                done.TrySetResult(true);
            }
        }

        /// <summary>
        /// 非同期セーブの列に並ぶ。戻り値は自分の番が来たら完了する。終わったらdoneを完了させて次へ番を渡すこと。
        /// </summary>
        System.Threading.Tasks.Task EnqueueSave(out System.Threading.Tasks.TaskCompletionSource<bool> done)
        {
            var turn = saveTail;
            done = new System.Threading.Tasks.TaskCompletionSource<bool>();
            saveTail = done.Task;
            return turn;
        }

        async Awaitable SaveBytesAsyncInternal(string filename, byte[] bytes)
        {
            if (IsCached(filename, bytes))
            {
                return;
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
#if UNITY_EDITOR
            catch (System.Exception)
            {
                throw;
            }
#else
            catch (System.Exception ex)
            {
                return LoadResult<T>.CreateError(ex);
            }
#endif
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