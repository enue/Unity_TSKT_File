using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class AssetBundleLoader<T> : CustomYieldInstruction
        where T : UnityEngine.Object
    {
        AssetBundleCreateRequest createRequest;
        AssetBundle assetBundle;
        AssetBundleRequest assetBundleRequest;
        readonly string name;

        bool error;

        public T[] AllAssets
        {
            get
            {
                Refresh();
                if (assetBundleRequest != null)
                {
                    if (assetBundleRequest.isDone)
                    {
                        var assets = assetBundleRequest.allAssets.Cast<T>().ToArray();
                        return assets;
                    }
                }
                return null;
            }
        }

        public T Asset
        {
            get
            {
                Refresh();

                if (assetBundleRequest != null)
                {
                    if (assetBundleRequest.isDone)
                    {
                        var asset = assetBundleRequest.asset as T;
                        Debug.Assert(asset, "failed to load asset : " + name);
                        if (!asset)
                        {
                            error = true;
                            assetBundleRequest = null;
                        }
                        return asset;
                    }
                }
                return null;
            }
        }

        void Refresh()
        {
            if (createRequest != null)
            {
                if (createRequest.isDone)
                {
                    assetBundle = createRequest.assetBundle;
                    Debug.Assert(assetBundle, "failed to load asset bundle: " + name);
                    if (!assetBundle)
                    {
                        error = true;
                    }
                    createRequest = null;
                }
            }
            if (assetBundle)
            {
                if (string.IsNullOrEmpty(name))
                {
                    assetBundleRequest = assetBundle.LoadAllAssetsAsync<T>();
                }
                else
                {
                    assetBundleRequest = assetBundle.LoadAssetAsync<T>(name);
                }
                assetBundle = null;
                LoadingProgress.Instance.Add(assetBundleRequest);
            }
        }

        public override bool keepWaiting
        {
            get
            {
                if (error)
                {
                    return false;
                }

                Refresh();

                if (assetBundleRequest != null)
                {
                    if (assetBundleRequest.isDone)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public AssetBundleLoader(string filename, string name, int priorityOffset)
        {
            this.name = name;
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            // アセットバンドルはLZ4圧縮（chunk based）にしておかないと遅い

            assetBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(_ => _.name == filename);
            if (!assetBundle)
            {
                createRequest = AssetBundle.LoadFromFileAsync(path);
                createRequest.priority += priorityOffset;
                LoadingProgress.Instance.Add(createRequest);
            }
        }
    }
}
