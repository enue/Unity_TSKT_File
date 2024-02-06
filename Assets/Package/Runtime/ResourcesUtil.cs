#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class ResourcesUtil
    {
        public static async Awaitable<T> LoadAsync<T>(string path, System.IProgress<float>? progress = null)
            where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            if (progress != null)
            {
                _ = Report(request, progress);
            }
            await request;
            return (T)request.asset;
        }

        static async Awaitable Report(ResourceRequest operation, System.IProgress<float> progress)
        {
            while (!operation.isDone)
            {
                progress.Report(operation.progress);
                await Awaitable.NextFrameAsync();
            }
            progress.Report(1f);
        }
    }
}
