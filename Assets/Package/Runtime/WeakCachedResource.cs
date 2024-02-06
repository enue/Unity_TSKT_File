#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    [System.Obsolete]
    public static class WeakCachedResource<T>
        where T : Object
    {
        public static void TrimCache()
        {
        }

        public static T? Load(string path)
        {
            return Resources.Load<T>(path);
        }

        public async static Awaitable<T?> LoadAsync(string path)
        {
            return await ResourcesUtil.LoadAsync<T>(path);
        }
        public static void Expire()
        {
        }
    }
}
