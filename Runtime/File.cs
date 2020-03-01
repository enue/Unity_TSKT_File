using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx.Async;

// Async保存は基本的に使わない。処理中にアプリを落とされるとファイルが壊れるからだ
// 対応としてはバックアップをとる、もしくは手動セーブの場合は使ってもいい。

namespace TSKT
{
    public class File
    {
        readonly static public string AppDirectory = Path.GetDirectoryName(Application.dataPath);
        public Files.ILoadSaveResolver Resolver { get; }
        public Files.ISerializeResolver SerialzieResolver { get; }

        public File(Files.ILoadSaveResolver resolver, Files.ISerializeResolver serializeResolver)
        {
            Resolver = resolver;
            SerialzieResolver = serializeResolver;
        }

        public byte[] Save<T>(string filename, T obj)
        {
            return Save(filename, obj, async: false).Result;
        }

        public UniTask<byte[]> SaveAsync<T>(string filename, T obj)
        {
            return Save(filename, obj, async: true);
        }

        async UniTask<byte[]> Save<T>(string filename, T obj, bool async)
        {
            byte[] bytes;
            if (async)
            {
                bytes = await UniTask.Run(() => SerialzieResolver.Serialize(obj));
            }
            else
            {
                bytes = SerialzieResolver.Serialize(obj);
            }

            await Resolver.SaveBytes(filename, bytes, async);
            return bytes;
        }

        public bool AnyExist(params string[] filenames)
        {
            if (filenames == null)
            {
                return false;
            }
            if (filenames.Length == 0)
            {
                return false;
            }
            return Resolver.AnyExist(filenames);
        }

        public LoadResult<T> Load<T>(string filename)
        {
            return Load<T>(filename, async: false).Result;
        }

        public UniTask<LoadResult<T>> LoadAsync<T>(string filename)
        {
            return Load<T>(filename, async: true);
        }

        async UniTask<LoadResult<T>> Load<T>(string filename, bool async)
        {
            byte[] bytes;
            try
            {
                var result = await Resolver.LoadBytes(filename, async);
                if (!result.Succeeded)
                {
                    return result.CreateFailed<T>();
                }
                bytes = result.value;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return LoadResult<T>.CreateError(ex);
            }

            try
            {
                if (async)
                {
                    var t = await UniTask.Run(() => SerialzieResolver.Deserialize<T>(bytes));
                    return new LoadResult<T>(t);
                }
                else
                {
                    var t = SerialzieResolver.Deserialize<T>(bytes);
                    return new LoadResult<T>(t);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(filename + " broken");
                Debug.LogException(ex);
                return LoadResult<T>.CreateFailedDeserialize(ex);
            }
        }
    }
}