using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class ResourceLoader<T> : CustomYieldInstruction
        where T : UnityEngine.Object
    {
        ResourceRequest request;
        readonly string path;

        T asset;
        public T Asset
        {
            get
            {
                if (request != null && request.isDone)
                {
                    asset = request.asset as T;
                    request = null;
                    Debug.Assert(Asset, "failed to load " + path);
                }
                return asset;
            }
        }

        public override bool keepWaiting
        {
            get
            {
                if (Asset)
                {
                    return false;
                }
                if (request == null)
                {
                    // ロードに失敗した場合、requestがnullになる
                    return false;
                }
                return true;
            }
        }

        public ResourceLoader(string path)
        {
#if CLOUD_BUILD || UNITY_IOS || UNITY_EDITOR_OSX
            path = path.Normalize(System.Text.NormalizationForm.FormD);
#endif
            request = Resources.LoadAsync<T>(path);
            LoadingProgress.Instance.Add(request);
            this.path = path;
        }
    }
}
