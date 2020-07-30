using System.Collections;
#if TSKT_FILE_ADDRESSABLE_SUPPORT

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public static class AddressableUtil
    {
        static async public UniTask<T> LoadAsync<T>(object key)
            where T : Object
        {
            var request = Addressables.LoadAssetAsync<T>(key);
            return await request.Task.AsUniTask();
        }
    }
}
#endif
