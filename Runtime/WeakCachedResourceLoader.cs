using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public class WeakCachedResourceLoader<T> : CustomYieldInstruction
        where T : Object
    {
        static readonly Dictionary<string, System.WeakReference> cache = new Dictionary<string, System.WeakReference>();

        UniTask<T> task;
        public T Asset => task.Result;

        public static void TrimCache()
        {
            foreach (var it in cache.ToArray())
            {
                if (!it.Value.IsAlive)
                {
                    cache.Remove(it.Key);
                }
            }
        }

        public override bool keepWaiting => !task.IsCompleted;

        public T Load(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                if (result.IsAlive)
                {
                    var asset = result.Target as T;
                    task = new UniTask<T>(asset);
                    return asset;
                }
            }

            task = new UniTask<T>(Resources.Load<T>(path));
            Debug.Assert(Asset, "asset not found : " + path);

            if (result != null)
            {
                result.Target = Asset;
            }
            else
            {
                cache.Add(path, new System.WeakReference(Asset));
            }
            return Asset;
        }

        public UniTask<T> LoadAsync(string path, System.Action<T> callback = null)
        {
            if (cache.TryGetValue(path, out var asset))
            {
                if (asset.IsAlive)
                {
                    task = new UniTask<T>(asset.Target as T);
                    callback?.Invoke(Asset);
                    return new UniTask<T>(Asset);
                }
            }

            task = LoadAsyncCoroutine(path);
            task.GetAwaiter().OnCompleted(() =>
            {
                cache[path] = new System.WeakReference(Asset);
                callback?.Invoke(Asset);
            });

            return task;
        }
        static async UniTask<T> LoadAsyncCoroutine(string path)
        {
            var loader = new ResourceLoader<T>(path);
            await loader;
            Debug.Assert(loader.Asset, "asset not found : " + path);
            return loader.Asset;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
