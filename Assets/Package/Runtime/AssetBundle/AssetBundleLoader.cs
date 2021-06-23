#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

#if TSKT_FILE_ASSETBUNDLE_SUPPORT

namespace TSKT
{
    public abstract class AssetBundleLoader
    {
        static int processCount;
        readonly static List<int> waitingProcessPriorities = new List<int>();

        protected abstract UniTask<LoadResult<AssetBundle?>> Load(string filename, uint crc = 0);

        public async UniTask<LoadResult<AssetBundle?>> LoadAssetBundle(
            string assetBundleName,
            string filepath,
            int priority,
            uint crc = 0)
        {
            {
                var assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == assetBundleName);
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
                return await LoadAssetBundle(assetBundleName, filepath: filepath, priority: priority, crc: crc);
            }

            ++processCount;
            try
            {
                return await Load(filepath, crc);
            }
            finally
            {
                --processCount;
            }
        }

        public async UniTask<LoadResult<T?>> LoadAsync<T>(string filename, string assetName, string filepath, int priority, uint crc = 0)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, filepath, priority, crc: crc);
            if (!assetBundle.Succeeded)
            {
                return new LoadResult<T?>(default, assetBundle.state, assetBundle.exception);
            }
            var assetBundleRequest = assetBundle.value!.LoadAssetAsync<T>(assetName);
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            var result = assetBundleRequest.asset as T;
            if (result)
            {
                return new LoadResult<T?>(result);
            }
            else
            {
                return LoadResult<T?>.CreateFailedDeserialize();
            }
        }

         public async UniTask<LoadResult<T[]?>> LoadAllAsync<T>(string filename, string filepath, int priority)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, filepath, priority);
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
