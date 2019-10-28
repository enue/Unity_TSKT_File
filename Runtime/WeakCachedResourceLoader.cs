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
        static readonly Dictionary<string, System.WeakReference<T>> cache = new Dictionary<string, System.WeakReference<T>>();

        UniTask<T> task;
        public T Asset => task.Result;

        public static void TrimCache()
        {
            foreach (var it in cache.ToArray())
            {
                if (!it.Value.TryGetTarget(out var _))
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
                if (result.TryGetTarget(out var asset))
                {
                    task = new UniTask<T>(asset);
                    return asset;
                }
            }

            task = new UniTask<T>(Resources.Load<T>(path));
            Debug.Assert(Asset, "asset not found : " + path);

            if (result != null)
            {
                result.SetTarget(Asset);
            }
            else
            {
                cache.Add(path, new System.WeakReference<T>(Asset));
            }
            return Asset;
        }

        public UniTask<T> LoadAsync(string path, System.Action<T> callback = null)
        {
            if (cache.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var asset))
                {
                    task = new UniTask<T>(asset);
                    callback?.Invoke(Asset);
                    return task;
                }
            }

            task = LoadAsyncCoroutine(path);
            task.GetAwaiter().OnCompleted(() =>
            {
                cache[path] = new System.WeakReference<T>(Asset);
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
