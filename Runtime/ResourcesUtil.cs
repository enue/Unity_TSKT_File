using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Async;

namespace TSKT
{
    public static class ResourcesUtil
    {
        static async public UniTask<T> LoadAsync<T>(string path)
            where T : Object
        {
#if CLOUD_BUILD || UNITY_IOS || UNITY_EDITOR_OSX
            path = path.Normalize(System.Text.NormalizationForm.FormD);
#endif
            var request = Resources.LoadAsync<T>(path);
            LoadingProgress.Instance.Add(request);
            await request;
            return request.asset as T;
        }
    }
}
