#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

#if TSKT_FILE_ASSETBUNDLE_SUPPORT

namespace TSKT
{
    public interface IAssetBundleLoadResolver
    {
        UniTask<LoadResult<AssetBundle?>> LoadAssetBundle(string filename, uint crc = 0);
    }

    public class WebAssetBundleResolver : IAssetBundleLoadResolver
    {
        Hash128? hash128;

        public async UniTask<LoadResult<AssetBundle?>> LoadAssetBundle(string filename, uint crc = 0)
        {
            using var request = hash128.HasValue
                ? UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(filename, hash128.Value, crc)
                : UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(filename, crc);

            var operation = request.SendWebRequest();
            LoadingProgress.Instance.Add(operation);
            await operation;
            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                return LoadResult<AssetBundle?>.CreateError();
            }

            var result = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
            if (!result)
            {
                return LoadResult<AssetBundle?>.CreateError();
            }
            return new LoadResult<AssetBundle?>(result);
        }
    }

    public class LocalAssetBundleLoadResolver : IAssetBundleLoadResolver
    {
        public async UniTask<LoadResult<AssetBundle?>> LoadAssetBundle(string filename, uint crc = 0)
        {
            var createRequest = AssetBundle.LoadFromFileAsync(filename, crc);
            LoadingProgress.Instance.Add(createRequest);

            await createRequest;
            var result = createRequest.assetBundle;
            if (!result)
            {
                return LoadResult<AssetBundle?>.CreateError();
            }
            return new LoadResult<AssetBundle?>(result);
        }
    }

    public class CryptedAssetBundleLoadResolver : IAssetBundleLoadResolver
    {
        public bool Web { get; set; }

        readonly string key;
        readonly byte[] salt;
        readonly int iteration;

        public CryptedAssetBundleLoadResolver(string key, byte[] salt, int iteration)
        {
            this.key = key;
            this.salt = salt;
            this.iteration = iteration;
        }

        public async UniTask<LoadResult<AssetBundle?>> LoadAssetBundle(string filename, uint crc = 0)
        {
            byte[] encryptedBytes;

            var webRequest = Web;
#if WEBGL && !UNITY_EDITOR
            webRequest = true;
#endif
            if (webRequest)
            {
                using var request = UnityEngine.Networking.UnityWebRequest.Get(filename);
                var operation = request.SendWebRequest();
                LoadingProgress.Instance.Add(operation);
                await operation;

                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    return LoadResult<AssetBundle?>.CreateError();
                }

                encryptedBytes = request.downloadHandler.data;
            }
            else
            {
                try
                {
                    using var file = System.IO.File.OpenRead(filename);
                    encryptedBytes = new byte[file.Length];
                    await file.ReadAsync(encryptedBytes, 0, encryptedBytes.Length).AsUniTask();
                }
                catch (System.IO.DirectoryNotFoundException ex)
                {
                    return LoadResult<AssetBundle?>.CreateNotFound(ex);
                }
                catch (System.IO.FileNotFoundException ex)
                {
                    return LoadResult<AssetBundle?>.CreateNotFound(ex);
                }
            }

            try
            {
#if UNITY_WEBGL
                var bytes = decryptor.Decrypt(CryptUtil.Decrypt(encryptedBytes, key, salt, iteration));
#else
                var bytes = await UniTask.Run(() => CryptUtil.Decrypt(encryptedBytes, key, salt, iteration));
#endif
                var request = AssetBundle.LoadFromMemoryAsync(bytes, crc);
                LoadingProgress.Instance.Add(request);
                await request;

                return new LoadResult<AssetBundle?>(request.assetBundle);
            }
            catch (System.Exception ex)
            {
                return LoadResult<AssetBundle?>.CreateFailedDeserialize(ex);
            }
        }
    }
}

#endif
