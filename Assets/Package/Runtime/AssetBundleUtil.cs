using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Security.Cryptography;

namespace TSKT
{
    public static class AssetBundleUtil
    {
        static int processCount;

        static async UniTask<AssetBundle> LoadAssetBundle(string filename, int priorityOffset,
            ICryptoTransform decryptor = null,
            string directory = null,
            uint crc = 0)
        {
            {
                var assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == filename);
                if (assetBundle)
                {
                    return assetBundle;
                }
            }

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
#if UNITY_WEBGL && !UNITY_EDITOR
                var request = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(path, crc);
                var operation = request.SendWebRequest();
                operation.priority += priorityOffset;
                LoadingProgress.Instance.Add(operation);
                await operation;
                if (webRequest.isHttpError)
                {
                    return null;
                }
                if (webRequest.isNetworkError)
                {
                    return null;
                }

                return UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
#else
                var createRequest = AssetBundle.LoadFromFileAsync(path, crc);
                createRequest.priority += priorityOffset;
                LoadingProgress.Instance.Add(createRequest);

                await createRequest;
                return createRequest.assetBundle;
#endif
            }
            else
            {
                if (processCount > 0)
                {
                    await UniTask.WaitWhile(() => processCount > 0);
                    return await LoadAssetBundle(filename, priorityOffset: priorityOffset, decryptor: decryptor, directory: directory, crc: crc);
                }

                ++processCount;
                try
                {
                    byte[] encryptedBytes;

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
                    using (var webRequest = UnityEngine.Networking.UnityWebRequest.Get(path))
                    {
                        var operation = webRequest.SendWebRequest();
                        operation.priority += priorityOffset;
                        LoadingProgress.Instance.Add(operation);
                        await operation;

                        if (webRequest.isHttpError)
                        {
                            return null;
                        }
                        if (webRequest.isNetworkError)
                        {
                            return null;
                        }

                        encryptedBytes = webRequest.downloadHandler.data;
                    }
#else
                    try
                    {
                        using (var file = System.IO.File.OpenRead(path))
                        {
                            encryptedBytes = new byte[file.Length];
                            await file.ReadAsync(encryptedBytes, 0, encryptedBytes.Length).AsUniTask();
                        }
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        return null;
                    }
#endif
#if UNITY_WEBGL
                    var bytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
#else
                    var bytes = await UniTask.Run(() => decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
#endif

                    var request = AssetBundle.LoadFromMemoryAsync(bytes, crc);
                    request.priority += priorityOffset;
                    LoadingProgress.Instance.Add(request);
                    await request;

                    return request.assetBundle;
                }
                finally
                {
                    --processCount;
                }
            }
        }

        static async public UniTask<T> LoadAsync<T>(string filename, string assetName, int priorityOffset,
            ICryptoTransform decryptor = null,
            string directory = null,
            uint crc = 0)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset, decryptor: decryptor, directory: directory, crc: crc);
            if (!assetBundle)
            {
                return null;
            }
            var assetBundleRequest = assetBundle.LoadAssetAsync<T>(assetName);
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            return assetBundleRequest.asset as T;
        }

        static async public UniTask<T[]> LoadAllAsync<T>(string filename, int priorityOffset, ICryptoTransform decryptor = null, string directory = null)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset, decryptor, directory);
            if (!assetBundle)
            {
                return null;
            }
            var assetBundleRequest = assetBundle.LoadAllAssetsAsync<T>();
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            return assetBundleRequest.allAssets.OfType<T>().ToArray();
        }
    }
}
