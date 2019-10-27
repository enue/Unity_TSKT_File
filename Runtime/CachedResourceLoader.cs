using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public static class CachedResourceLoader<T>
        where T : Object
    {
        readonly static Dictionary<string, T> cache = new Dictionary<string, T>();

        public static T Load(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                return result;
            }

            result = Resources.Load<T>(path);
            Debug.Assert(result, "not found entity : " + path);

            cache.Add(path, result);
            return result;
        }

        async static public UniTask<T> LoadAsync(string path)
        {
            if (cache.TryGetValue(path, out var asset))
            {
                return asset;
            }
            var loader = new ResourceLoader<T>(path);
            await loader;
            var result = loader.Asset;
            Debug.Assert(result, "entity not found : " + path);
            cache[path] = result;

            return result;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
