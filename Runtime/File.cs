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
    public static class File
    {
        readonly static public string AppDirectory = Path.GetDirectoryName(Application.dataPath);
        public static Files.IResolver Resolver { get; set; }

        public static void SaveBytes(string filename, byte[] data)
        {
            SaveBytes(filename, data, async: false).Forget();
        }
        public static UniTask SaveBytesAsync(string filename, byte[] data)
        {
            return SaveBytes(filename, data, async: true);
        }

        static UniTask SaveBytes(string filename, byte[] data, bool async)
        {
            return Resolver.SaveBytes(filename, data, async);
        }

        static public void SaveString(string filename, string data)
        {
            SaveString(filename, data, async: false).Forget();
        }

        static public UniTask SaveStringAsync(string filename, string data)
        {
            return SaveString(filename, data, async: true);
        }

        static async UniTask SaveString(string filename, string data, bool async)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            await SaveBytes(filename, bytes, async: async);
        }

        static public void Save<T>(string filename, T obj, bool crypt)
        {
            Save(filename, obj, crypt, async: false).Forget();
        }

        static public UniTask SaveAsync<T>(string filename, T obj, bool crypt)
        {
            return Save(filename, obj, crypt, async: true);
        }

        static async UniTask Save<T>(string filename, T obj, bool crypt, bool async)
        {
            if (async)
            {
                await UniTask.SwitchToTaskPool();
            }

            var bytes = Resolver.Serialize(obj);
            if (crypt)
            {
                bytes = Resolver.Encrypt(bytes);
            }

            if (async)
            {
                await UniTask.SwitchToMainThread();
            }

            await SaveBytes(filename, bytes, async: async);
        }

        public static bool AnyExist(params string[] filenames)
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

        public static Option<byte[]> LoadBytes(string filename)
        {
            return LoadBytes(filename, async: false).Result;
        }
        public static UniTask<Option<byte[]>> LoadBytesAsync(string filename)
        {
            return LoadBytes(filename, async: true);
        }

        static UniTask<Option<byte[]>> LoadBytes(string filename, bool async)
        {
            return Resolver.LoadBytes(filename, async);
        }

        public static Option<string> LoadString(string filename)
        {
            return LoadString(filename, async: false).Result;
        }

        public static UniTask<Option<string>> LoadStringAsync(string filename)
        {
            return LoadString(filename, async: true);
        }

        static async UniTask<Option<string>> LoadString(string filename, bool async)
        {
            var result = await LoadBytes(filename, async);
            if (!result.hasValue)
            {
                return Option<string>.Empty;
            }
            return new Option<string>(System.Text.Encoding.UTF8.GetString(result.value));
        }

        public static Option<T> Load<T>(string filename, bool decrypt)
        {
            return Load<T>(filename, decrypt: decrypt, async: false).Result;
        }

        public static UniTask<Option<T>> LoadAsync<T>(string filename, bool decrypt)
        {
            return Load<T>(filename, decrypt: decrypt, async: true);
        }

        async static UniTask<Option<T>> Load<T>(string filename, bool decrypt, bool async)
        {
            try
            {
                var result = await LoadBytes(filename, async);
                if (!result.hasValue)
                {
                    return Option<T>.Empty;
                }

                var bytes = result.value;
                if (decrypt)
                {
                    if (async)
                    {
                        bytes = await UniTask.Run(() => Resolver.Decrypt(bytes));
                    }
                    else
                    {
                        bytes = Resolver.Decrypt(bytes);
                    }
                    if (bytes == null)
                    {
                        return Option<T>.Empty;
                    }
                }
                if (async)
                {
                    var t = await UniTask.Run(() => Resolver.Deserialize<T>(bytes));
                    return new Option<T>(t);
                }
                else
                {
                    var t = Resolver.Deserialize<T>(bytes);
                    return new Option<T>(t);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(filename + " broken");
                Debug.LogException(ex);
                return Option<T>.Empty;
            }
        }
    }
}