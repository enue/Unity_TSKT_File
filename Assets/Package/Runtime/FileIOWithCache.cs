#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public class FileIOWithCache<T>
    {
        public FileIO FileIO { get; }
        readonly Dictionary<string, LoadResult<T>> cache = new Dictionary<string, LoadResult<T>>();

        public Files.ILoadSaveResolver Resolver => FileIO.Resolver;
        public Files.ISerializeResolver SerialzieResolver => FileIO.SerialzieResolver;

        public FileIOWithCache(FileIO fileIO)
        {
            FileIO = fileIO;
        }

        public FileIOWithCache(Files.ILoadSaveResolver resolver, Files.ISerializeResolver serializeResolver)
        {
            FileIO = new FileIO(resolver: resolver, serializeResolver: serializeResolver);
        }

        public byte[] Save(string filename, T obj)
        {
            lock (cache)
            {
                cache[filename] = new LoadResult<T>(obj);
            }
            return FileIO.Save(filename, obj);
        }

        public UniTask<byte[]> SaveAsync(string filename, T obj, System.IProgress<float>? progress = null)
        {
            lock (cache)
            {
                cache[filename] = new LoadResult<T>(obj);
            }
            return FileIO.SaveAsync(filename, obj, progress);
        }

        public bool AnyExist(params string[] filenames)
        {
            lock (cache)
            {
                foreach (var it in filenames)
                {
                    if (cache.TryGetValue(it, out var value))
                    {
                        if (value.state == LoadResult.State.Succeeded)
                        {
                             return true;
                        }
                    }
                }
            }

            return FileIO.AnyExist();
        }

        public async UniTask<LoadResult<T>> LoadAsync(string filename, System.IProgress<float>? progress = null)
        {
            lock (cache)
            {
                if (cache.TryGetValue(filename, out var result))
                {
                    progress?.Report(1f);
                    return result;
                }
            }
            var loadResult = await FileIO.LoadAsync<T>(filename, progress);
            lock (cache)
            {
                cache[filename] = loadResult;
            }
            return loadResult;
        }

        public LoadResult<T> Load(string filename)
        {
            lock (cache)
            {
                if (cache.TryGetValue(filename, out var result))
                {
                    return result;
                }
            }
            var loadResult = FileIO.Load<T>(filename);
            lock (cache)
            {
                cache[filename] = loadResult;
            }
            return loadResult;
        }

        public void PseudoDelete(string filename)
        {
            lock (cache)
            {
                cache[filename] = LoadResult<T>.CreateNotFound();
            }
        }
    }
}
