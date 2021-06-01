#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public static class CachedResource<T>
        where T : Object
    {
        readonly static Dictionary<string, T?> cache = new Dictionary<string, T?>();

        static public T? Load(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                if (result)
                {
                    return result;
                }
            }

            var asset = Resources.Load<T>(path);
            Debug.Assert(asset, "asset not found : " + path);

            cache.Add(path, asset);
            return asset;
        }

        async static public UniTask<T?> LoadAsync(string path)
        {
            if (cache.TryGetValue(path, out var asset))
            {
                if (asset)
                {
                    return asset;
                }
            }

            var result = await ResourcesUtil.LoadAsync<T>(path);
            cache[path] = result;

            return result;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
