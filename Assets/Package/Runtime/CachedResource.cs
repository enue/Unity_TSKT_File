#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class CachedResource<T>
        where T : Object
    {
        static CachedResource<T>? instance;
        static CachedResource<T> Instance => instance ??= new();
        readonly Dictionary<string, T?> cache = new();

        CachedResource()
        {
            Application.exitCancellationToken.Register(() => instance = null);
        }

        public static T? Load(string path)
        {
            if (Instance.cache.TryGetValue(path, out var result))
            {
                if (result)
                {
                    return result;
                }
            }

            result = Resources.Load<T>(path);

            Instance.cache[path] = result;
            return result;
        }

        public async static Awaitable<T?> LoadAsync(string path)
        {
            if (Instance.cache.TryGetValue(path, out var result))
            {
                if (result)
                {
                    return result;
                }
            }

            result = await ResourcesUtil.LoadAsync<T>(path, _ => LoadingProgress.Instance.Observe(_));
            Instance.cache[path] = result;

            return result;
        }

        public static void Expire()
        {
            Instance.cache.Clear();
        }
    }
}
