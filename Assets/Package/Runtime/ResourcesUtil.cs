#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public static class ResourcesUtil
    {
        static async public UniTask<T?> LoadAsync<T>(string path)
            where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            LoadingProgress.Instance.Add(request);
            await request;
            return request.asset as T;
        }
    }
}
