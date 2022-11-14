#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;

namespace TSKT
{
    public class FileIO
    {
        readonly Dictionary<string, LoadResult<byte[]>> cache = new();
        public Files.ILoadSaveResolver Resolver { get; }
        public Files.ISerializeResolver SerialzieResolver { get; }

        public FileIO(Files.ILoadSaveResolver resolver, Files.ISerializeResolver serializeResolver)
        {
            Resolver = resolver;
            SerialzieResolver = serializeResolver;
        }

        public ReadOnlySpan<byte> Save<T>(string filename, T obj)
        {
            var bytes = SerialzieResolver.Serialize(obj);
            Resolver.SaveBytes(filename, bytes);
            cache[filename] = new(bytes.ToArray());
            return bytes;
        }

        /// <summary>
        /// シリアライズからファイルアクセスまですべて非同期で行う。途中でタスクキルされるとファイルが壊れることがあるので注意。対応済みの場合のみ採用すること
        /// </summary>
        public async UniTask<byte[]> SaveAsync<T>(string filename, T obj, System.IProgress<float>? progress = null)
        {
            try
            {
                var bytes = await SerialzieResolver.SerializeAsync(obj);
                progress?.Report(0.5f);
                using (new PreventFromQuitting(null))
                {
                    await Resolver.SaveBytesAsync(filename, bytes);
                }
                cache[filename] = new(bytes);
                return bytes;
            }
            finally
            {
                progress?.Report(1f);
            }
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

        public async UniTask<LoadResult<T>> LoadAsync<T>(string filename, System.IProgress<float>? progress = null)
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
                    var t = await SerialzieResolver.DeserializeAsync<T>(bytes.value);
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
                var t = SerialzieResolver.Deserialize<T>(bytes.value);
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