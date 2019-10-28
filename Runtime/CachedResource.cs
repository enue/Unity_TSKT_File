using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public static class CachedResource<T>
        where T : Object
    {
        readonly static Dictionary<string, T> cache = new Dictionary<string, T>();

        static public T Load(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                return result;
            }

            var asset = Resources.Load<T>(path);
            Debug.Assert(asset, "asset not found : " + path);

            cache.Add(path, asset);
            return asset;
        }

        static public UniTask<T> LoadAsync(string path)
        {
            if (cache.TryGetValue(path, out var asset))
            {
                return new UniTask<T>(asset);
            }

            var task = ResourcesUtil.LoadAsync<T>(path);
            task.GetAwaiter().OnCompleted(() =>
            {
                cache[path] = task.Result;
            });

            return task;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
