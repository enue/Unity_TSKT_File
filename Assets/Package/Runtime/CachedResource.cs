#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    [System.Obsolete]
    public static class CachedResource<T>
        where T : Object
    {

        public static T? Load(string path)
        {
            return Resources.Load<T>(path);
        }

        public async static Awaitable<T?> LoadAsync(string path)
        {
            return await ResourcesUtil.LoadAsync<T>(path, _ => LoadingProgress.Instance.Observe(_));
        }

        public static void Expire()
        {
        }
    }
}
