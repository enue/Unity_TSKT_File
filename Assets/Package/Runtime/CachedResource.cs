﻿#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public static class CachedResource<T>
        where T : Object
    {
        readonly static Dictionary<string, T?> cache = new();

        public static T? Load(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                if (result)
                {
                    return result;
                }
            }

            result = Resources.Load<T>(path);

            cache[path] = result;
            return result;
        }

        public async static Awaitable<T?> LoadAsync(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                if (result)
                {
                    return result;
                }
            }

            result = await ResourcesUtil.LoadAsync<T>(path);
            cache[path] = result;

            return result;
        }

        public static void Expire()
        {
            cache.Clear();
        }
    }
}
