using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public static class AssetBundleUtil
    {
        static async UniTask<AssetBundle> LoadAssetBundle(string filename, int priorityOffset)
        {
            var assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == filename);
            if (!assetBundle)
            {
                var path = System.IO.Path.Combine(Application.streamingAssetsPath, filename);
                var createRequest = AssetBundle.LoadFromFileAsync(path);
                createRequest.priority += priorityOffset;
                LoadingProgress.Instance.Add(createRequest);

                await createRequest;
                assetBundle = createRequest.assetBundle;
            }
            return assetBundle;
        }

        static async public UniTask<T> LoadAsync<T>(string filename, string assetName, int priorityOffset)
            where T : UnityEngine.Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset);
            var assetBundleRequest = assetBundle.LoadAssetAsync<T>(assetName);
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            return assetBundleRequest.asset as T;
        }

        static async public UniTask<T[]> LoadAllAsync<T>(string filename, int priorityOffset)
            where T : UnityEngine.Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset);
            var assetBundleRequest = assetBundle.LoadAllAssetsAsync<T>();
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            return assetBundleRequest.allAssets.OfType<T>().ToArray();
        }
    }
}
