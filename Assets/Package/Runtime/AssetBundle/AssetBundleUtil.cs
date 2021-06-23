#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

#if TSKT_FILE_ASSETBUNDLE_SUPPORT

namespace TSKT
{
    public static class AssetBundleUtil
    {
        static int processCount;
        readonly static List<int> waitingProcessPriorities = new List<int>();

        static async UniTask<LoadResult<AssetBundle?>> LoadAssetBundle(
            string filename,
            int priority,
            IAssetBundleLoadResolver loader,
            string? directory = null,
            uint crc = 0)
        {
            {
                var assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == filename);
                if (assetBundle)
                {
                    return new LoadResult<AssetBundle?>(assetBundle);
                }
            }

            if (processCount > 0)
            {
                waitingProcessPriorities.Add(priority);
                waitingProcessPriorities.Sort();
                await UniTask.WaitWhile(() => processCount > 0 || waitingProcessPriorities[waitingProcessPriorities.Count - 1] > priority);
                waitingProcessPriorities.Remove(priority);
                return await LoadAssetBundle(filename, priority: priority, loader: loader, directory: directory, crc: crc);
            }

            ++processCount;
            try
            {
                string path;
                if (directory == null)
                {
                    path = filename;
                }
                else
                {
                    path = System.IO.Path.Combine(directory, filename);
                }
                return await loader.LoadAssetBundle(path, crc);
            }
            finally
            {
                --processCount;
            }
        }

        static async public UniTask<LoadResult<T?>> LoadAsync<T>(string filename, string assetName, int priority,
            string? directory = null,
            uint crc = 0)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priority, directory: directory, crc: crc);
            if (!assetBundle.Succeeded)
            {
                return new LoadResult<T?>(default, assetBundle.state, assetBundle.exception);
            }
            var assetBundleRequest = assetBundle.value!.LoadAssetAsync<T>(assetName);
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            var result = assetBundleRequest.asset as T;
            return new LoadResult<T?>(result);
        }

        static async public UniTask<LoadResult<T[]?>> LoadAllAsync<T>(string filename, int priority, string? directory = null)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priority, directory);
            if (!assetBundle.Succeeded)
            {
                return new LoadResult<T[]?>(default, assetBundle.state, assetBundle.exception);
            }
            var assetBundleRequest = assetBundle.value!.LoadAllAssetsAsync<T>();
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;
            var result = assetBundleRequest.allAssets.OfType<T>().ToArray();
            return new LoadResult<T[]?>(result);
        }
    }
}

#endif
