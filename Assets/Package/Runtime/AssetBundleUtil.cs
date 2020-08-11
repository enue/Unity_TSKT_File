﻿using System.Collections;
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

        static async UniTask<LoadResult<AssetBundle>> LoadAssetBundle(string filename, int priorityOffset,
            ICryptoTransform decryptor = null,
            string directory = null,
            uint crc = 0)
        {
            {
                var assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == filename);
                if (assetBundle)
                {
                    return new LoadResult<AssetBundle>(assetBundle);
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
                if (request.isHttpError)
                {
                    return LoadResult<AssetBundle>.CreateError();
                }
                if (request.isNetworkError)
                {
                    return LoadResult<AssetBundle>.CreateError();
                }

                var result = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
#else
                var createRequest = AssetBundle.LoadFromFileAsync(path, crc);
                createRequest.priority += priorityOffset;
                LoadingProgress.Instance.Add(createRequest);

                await createRequest;
                var result = createRequest.assetBundle;
#endif
                if (!result)
                {
                    return LoadResult<AssetBundle>.CreateError();
                }
                return new LoadResult<AssetBundle>(result);
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
                            return LoadResult<AssetBundle>.CreateError();
                        }
                        if (webRequest.isNetworkError)
                        {
                            return LoadResult<AssetBundle>.CreateError();
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
                    catch (System.IO.DirectoryNotFoundException ex)
                    {
                        return LoadResult<AssetBundle>.CreateNotFound(ex);
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        return LoadResult<AssetBundle>.CreateNotFound(ex);
                    }
                    catch (System.Exception ex)
                    {
                        return LoadResult<AssetBundle>.CreateError(ex);
                    }
#endif

                    try
                    {
#if UNITY_WEBGL
                        var bytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
#else
                        var bytes = await UniTask.Run(() => decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
#endif
                        var request = AssetBundle.LoadFromMemoryAsync(bytes, crc);
                        request.priority += priorityOffset;
                        LoadingProgress.Instance.Add(request);
                        await request;

                        return new LoadResult<AssetBundle>(request.assetBundle);
                    }
                    catch (System.Exception ex)
                    {
                        return LoadResult<AssetBundle>.CreateFailedDeserialize(ex);
                    }
                }
                finally
                {
                    --processCount;
                }
            }
        }

        static async public UniTask<LoadResult<T>> LoadAsync<T>(string filename, string assetName, int priorityOffset,
            ICryptoTransform decryptor = null,
            string directory = null,
            uint crc = 0)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset, decryptor: decryptor, directory: directory, crc: crc);
            if (!assetBundle.Succeeded)
            {
                return new LoadResult<T>(default, assetBundle.state, assetBundle.exception);
            }
            var assetBundleRequest = assetBundle.value.LoadAssetAsync<T>(assetName);
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;

            var result = assetBundleRequest.asset as T;
            return new LoadResult<T>(result);
        }

        static async public UniTask<LoadResult<T[]>> LoadAllAsync<T>(string filename, int priorityOffset, ICryptoTransform decryptor = null, string directory = null)
            where T : Object
        {
            var assetBundle = await LoadAssetBundle(filename, priorityOffset, decryptor, directory);
            if (!assetBundle.Succeeded)
            {
                return new LoadResult<T[]>(default, assetBundle.state, assetBundle.exception);
            }
            var assetBundleRequest = assetBundle.value.LoadAllAssetsAsync<T>();
            LoadingProgress.Instance.Add(assetBundleRequest);
            await assetBundleRequest;
            var result = assetBundleRequest.allAssets.OfType<T>().ToArray();
            return new LoadResult<T[]>(result);
        }
    }
}
