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
        readonly FileIO fileIO;
        readonly Dictionary<string, LoadResult<T>> cache = new Dictionary<string, LoadResult<T>>();
        public FileIOWithCache(FileIO fileIO)
        {
            this.fileIO = fileIO;
        }

        public byte[] Save(string filename, T obj)
        {
            lock (cache)
            {
                cache[filename] = new LoadResult<T>(obj);
            }
            return fileIO.Save(filename, obj);
        }

        public UniTask<byte[]> SaveAsync(string filename, T obj)
        {
            lock (cache)
            {
                cache[filename] = new LoadResult<T>(obj);
            }
            return fileIO.SaveAsync(filename, obj);
        }

        public UniTask<byte[]> SaveWhollyAsync(string filename, T obj)
        {
            lock (cache)
            {
                cache[filename] = new LoadResult<T>(obj);
            }
            return fileIO.SaveWhollyAsync(filename, obj);
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

            return fileIO.AnyExist();
        }

        public async UniTask<LoadResult<T>> LoadAsync(string filename)
        {
            lock (cache)
            {
                if (cache.TryGetValue(filename, out var result))
                {
                    return result;
                }
            }
            var loadResult = await fileIO.LoadAsync<T>(filename);
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
            var loadResult = fileIO.Load<T>(filename);
            lock (cache)
            {
                cache[filename] = loadResult;
            }
            return loadResult;
        }
    }
}
