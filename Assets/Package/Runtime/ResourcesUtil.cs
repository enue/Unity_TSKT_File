#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class ResourcesUtil
    {
        public static async Awaitable<T> LoadAsync<T>(string path, System.Action<ResourceRequest> beforeComplete)
            where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            beforeComplete?.Invoke(request);
            await request;
            return (T)request.asset;
        }

        public static async Awaitable<T> LoadAsync<T>(string path, System.IProgress<float>? progress = null)
            where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            progress?.Report(0.1f);
            await request;
            progress?.Report(1f);
            return (T)request.asset;
        }
    }
}
