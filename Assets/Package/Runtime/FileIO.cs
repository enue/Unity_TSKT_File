﻿#nullable enable
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
        /// シリアライズからファイルアクセスまですべて非同期で行う。途中でタスクキルされるとファイルが壊れることがあるので注意。対応済みの場合のみ採用すること
        /// </summary>
        public async UniTask<byte[]> SaveAsync<T>(string filename, T obj, System.IProgress<float>? progress = null)
        {
            try
            {
                var bytes = await SerialzieResolver.SerializeAsync(obj);
                progress?.Report(0.5f);
                using (new PreventFromQuitting(null))
                {
                    await Resolver.SaveBytesAsync(filename, bytes);
                }
                return bytes;
            }
            finally
            {
                progress?.Report(1f);
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

        public async UniTask<LoadResult<T>> LoadAsync<T>(string filename, System.IProgress<float>? progress = null)
        {
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

                progress?.Report(0.5f);

                try
                {
                    var t = await SerialzieResolver.DeserializeAsync<T>(bytes);
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
                progress?.Report(1f);
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