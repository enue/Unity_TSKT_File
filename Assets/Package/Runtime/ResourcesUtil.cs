#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public static class ResourcesUtil
    {
        static async public UniTask<T?> LoadAsync<T>(string path, System.IProgress<float>? progress = null)
            where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            await request.ToUniTask(progress);
            return request.asset as T;
        }
    }
}
