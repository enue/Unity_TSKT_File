using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public class CachedResourceLoader<T> : CustomYieldInstruction
        where T : Object
    {
        readonly static Dictionary<string, T> cache = new Dictionary<string, T>();

        UniTask<T> task;
        public T Asset => task.Result;

        public override bool keepWaiting => !task.IsCompleted;

        public T Load(string path)
        {
            if (cache.TryGetValue(path, out var result))
            {
                task = new UniTask<T>(result);
                return Asset;
            }

            task = new UniTask<T>(Resources.Load<T>(path));
            Debug.Assert(Asset, "asset not found : " + path);

            cache.Add(path, Asset);
            return Asset;
        }

        public UniTask<T> LoadAsync(string path, System.Action<T> callback = null)
        {
            if (cache.TryGetValue(path, out var asset))
            {
                task = new UniTask<T>(asset);
                callback?.Invoke(asset);
                return new UniTask<T>(asset);
            }

            task = LoadAsyncCoroutine(path);
            task.GetAwaiter().OnCompleted(() =>
            {
                cache[path] = Asset;
                callback?.Invoke(Asset);
            });

            return task;
        }

        async static UniTask<T> LoadAsyncCoroutine(string path)
        {
            var loader = new ResourceLoader<T>(path);
            await loader;
            var asset = loader.Asset;
            Debug.Assert(asset, "asset not found : " + path);
            return asset;
        }

        static public void Expire()
        {
            cache.Clear();
        }
    }
}
