using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public static class WeakCachedResourceLoader<T>
        where T : Object
    {
        static readonly Dictionary<string, System.WeakReference<T>> cache = new Dictionary<string, System.WeakReference<T>>();

        public static void TrimCache()
        {
            foreach (var it in cache.ToArray())
            {
                if (!it.Value.TryGetTarget(out var t))
                {
                    cache.Remove(it.Key);
                }
            }
        }

        public static T Load(string path)
        {
            if (cache.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var instance))
                {
                    if (instance)
                    {
                        return instance;
                    }
                }
            }

            var result = Resources.Load<T>(path);
            Debug.Assert(result, "not found entity : " + path);

            if (weakRef != null)
            {
                weakRef.SetTarget(result);
            }
            else
            {
                cache.Add(path, new System.WeakReference<T>(result));
            }
            return result;
        }

        async public static UniTask<T> LoadAsync(string path)
        {
            if (cache.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var t))
                {
                    if (t)
                    {
                        return t;
                    }
                }
            }
            var loader = new ResourceLoader<T>(path);
            await loader;
            var result = loader.Asset;
            Debug.Assert(result, "entity not found : " + path);
            cache[path] = new System.WeakReference<T>(result);
            return result;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
