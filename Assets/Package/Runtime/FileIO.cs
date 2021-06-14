using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public class FileIO
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        readonly static public string AppDirectory = Path.GetDirectoryName(Application.dataPath);
#else
        public static string AppDirectory => Application.persistentDataPath;
#endif

        public Files.ILoadSaveResolver Resolver { get; }
        public Files.ISerializeResolver SerialzieResolver { get; }

        public FileIO(Files.ILoadSaveResolver resolver, Files.ISerializeResolver serializeResolver)
        {
            Resolver = resolver;
            SerialzieResolver = serializeResolver;
        }

        public byte[] Save<T>(string filename, T obj)
        {
            var bytes = SerialzieResolver.Serialize(obj);
            Resolver.SaveBytes(filename, bytes);
            return bytes;
        }

        /// <summary>
        /// シリアライズのみ非同期で、ファイルアクセスは同期的に処理する。タスクキルによるファイル破損を気にする必要がない
        /// </summary>
        public async UniTask<byte[]> SaveAsync<T>(string filename, T obj)
        {
            var progress = LoadingProgress.Instance.Add();

            try
            {
#if UNITY_WEBGL
                var bytes = SerialzieResolver.Serialize(obj);
#else
                var bytes = await UniTask.Run(() => SerialzieResolver.Serialize(obj));
#endif
                Resolver.SaveBytes(filename, bytes);
                return bytes;
            }
            finally
            {
                progress.Report(1f);
            }
        }

        /// <summary>
        /// シリアライズからファイルアクセスまですべて非同期で行う。途中でタスクキルされるとファイルが壊れることがあるので注意。対応済みの場合のみ採用すること
        /// </summary>
        public async UniTask<byte[]> SaveWhollyAsync<T>(string filename, T obj)
        {
            var progress = LoadingProgress.Instance.Add();

            try
            {
#if UNITY_WEBGL
                var bytes = SerialzieResolver.Serialize(obj);
#else
                var bytes = await UniTask.Run(() => SerialzieResolver.Serialize(obj));
#endif
                progress.Report(0.5f);

                await Resolver.SaveBytesAsync(filename, bytes);
                return bytes;
            }
            finally
            {
                progress.Report(1f);
            }
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

        public async UniTask<LoadResult<T>> LoadAsync<T>(string filename)
        {
            var progress = LoadingProgress.Instance.Add();

            try
            {
                byte[] bytes;
                try
                {
                    var result = await Resolver.LoadBytesAsync(filename);
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

                progress.Report(0.5f);

                try
                {
#if UNITY_WEBGL
                    var t = SerialzieResolver.Deserialize<T>(bytes);
#else
                    var t = await UniTask.Run(() => SerialzieResolver.Deserialize<T>(bytes));
#endif
                    return new LoadResult<T>(t);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(filename + " broken");
                    Debug.LogException(ex);
                    return LoadResult<T>.CreateFailedDeserialize(ex);
                }
            }
            finally
            {
                progress.Report(1f);
            }
        }

        public LoadResult<T> Load<T>(string filename)
        {
            byte[] bytes;
            try
            {
                var result = Resolver.LoadBytes(filename);
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
                var t = SerialzieResolver.Deserialize<T>(bytes);
                return new LoadResult<T>(t);
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