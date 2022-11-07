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
            var result = await Resources.LoadAsync<T>(path);
            progress?.Report(1f);
            return result as T;
        }
    }
}
