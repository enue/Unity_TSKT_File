using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;
using System.Security.Cryptography;

namespace TSKT
{
    public static class AssetBundleUtil
    {
        static async UniTask<AssetBundle> LoadAssetBundle(string filename, int priorityOffset, ICryptoTransform decryptor = null, string directory = null)
        {
            var assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == filename);
            if (!assetBundle)
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

                if (decryptor == null)
                {
                    var createRequest = AssetBundle.LoadFromFileAsync(path);
                    createRequest.priority += priorityOffset;
                    LoadingProgress.Instance.Add(createRequest);

                    await createRequest;
                    assetBundle = createRequest.assetBundle;
                }
                else
                {
                    using (var file = System.IO.File.OpenRead(path))
                    {
                        var encryptedBytes = new byte[file.Length];
                        await file.ReadAsync(encryptedBytes, 0, encryptedBytes.Length).AsUniTask();

                        await UniTask.SwitchToThreadPool();
                        var bytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        await UniTask.SwitchToMainThread();

                        var request = AssetBundle.LoadFromMemoryAsync(bytes);
                        request.priority += priorityOffset;
                        LoadingProgress.Instance.Add(request);
                        await request;

                        assetBundle = request.assetBundle;
                    }
                }
            }
            return assetBundle;
        }

        static async public UniTask<T> LoadAsync<T>(string filename, string assetName, int priorityOffset, ICryptoTransform decryptor = null, string directory = null)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset, decryptor, directory);
            var assetBundleRequest = assetBundle.LoadAssetAsync<T>(assetName);
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            return assetBundleRequest.asset as T;
        }

        static async public UniTask<T[]> LoadAllAsync<T>(string filename, int priorityOffset, ICryptoTransform decryptor = null, string directory = null)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset, decryptor, directory);
            var assetBundleRequest = assetBundle.LoadAllAssetsAsync<T>();
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            return assetBundleRequest.allAssets.OfType<T>().ToArray();
        }
    }
}
