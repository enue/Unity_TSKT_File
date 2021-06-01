#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public static class WeakCachedResource<T>
        where T : Object
    {
        static readonly Dictionary<string, System.WeakReference<T>> cache = new Dictionary<string, System.WeakReference<T>>();

        public static void TrimCache()
        {
            foreach (var it in cache.ToArray())
            {
                if (!it.Value.TryGetTarget(out var _))
                {
                    cache.Remove(it.Key);
                }
            }
        }

        public static T? Load(string path)
        {
            if (cache.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var asset))
                {
                    if (asset)
                    {
                        return asset;
                    }
                }
            }

            var result = Resources.Load<T>(path);
            Debug.Assert(result, "asset not found : " + path);

            if (result)
            {
                if (weakRef != null)
                {
                    weakRef.SetTarget(result);
                }
                else
                {
                    cache.Add(path, new System.WeakReference<T>(result));
                }
            }
            return result;
        }

        public async static UniTask<T?> LoadAsync(string path)
        {
            if (cache.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var asset))
                {
                    if (asset)
                    {
                        return asset;
                    }
                }
            }

            var result = await ResourcesUtil.LoadAsync<T>(path);
            if (result)
            {
                cache[path] = new System.WeakReference<T>(result!);
            }

            return result;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
