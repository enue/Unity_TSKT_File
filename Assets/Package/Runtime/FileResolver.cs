﻿using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TSKT.Files
{
    public interface ILoadSaveResolver
    {
        bool AnyExist(params string[] filenames);
        UniTask<LoadResult<byte[]>> LoadBytes(string filename, bool async);
        UniTask SaveBytes(string filename, byte[] data, bool async);
    }

    public interface ISerializeResolver
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] bytes);
    }

    public class DefaultResolver : ILoadSaveResolver
    {
        readonly string directory;

        public DefaultResolver(string directory)
        {
            this.directory = directory;
        }

        string GetPath(string filename)
        {
            string dir;
            if (directory == null)
            {
                dir = File.AppDirectory;
            }
            else
            {
                dir = Path.Combine(File.AppDirectory, directory);
            }
            return Path.Combine(dir, filename);
        }

        public bool AnyExist(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                if (System.IO.File.Exists(GetPath(filename)))
                {
                    return true;
                }
            }
            return false;
        }

        public async UniTask SaveBytes(string filename, byte[] data, bool async)
        {
            var fullPath = GetPath(filename);
            CreateDictionary(fullPath);
            if (async)
            {
                using (var file = System.IO.File.Open(fullPath, FileMode.Create))
                {
                    await file.WriteAsync(data, 0, data.Length).AsUniTask();
                }
            }
            else
            {
                System.IO.File.WriteAllBytes(fullPath, data);
            }
        }

        public async UniTask<LoadResult<byte[]>> LoadBytes(string filename, bool async)
        {
            var fullPath = GetPath(filename);

            if (!System.IO.File.Exists(fullPath))
            {
                return LoadResult<byte[]>.CreateNotFound();
            }
            if (async)
            {
                using (var fileStream = System.IO.File.OpenRead(fullPath))
                {
                    var bytes = new byte[fileStream.Length];
                    await fileStream.ReadAsync(bytes, 0, bytes.Length).AsUniTask();
                    return new LoadResult<byte[]>(bytes);
                }
            }
            else
            {
                return new LoadResult<byte[]>(System.IO.File.ReadAllBytes(fullPath));
            }
        }

        static void CreateDictionary(string fullPath)
        {
            var index = Mathf.Max(
                fullPath.LastIndexOf(Path.DirectorySeparatorChar),
                fullPath.LastIndexOf(Path.AltDirectorySeparatorChar));
            var dir = fullPath.Substring(0, index);
            Directory.CreateDirectory(dir);
        }
    }

    public class PrefsResolver : ILoadSaveResolver
    {
        public bool AnyExist(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                if (PlayerPrefs.HasKey(filename))
                {
                    return true;
                }
            }
            return false;
        }

        public UniTask SaveBytes(string filename, byte[] data, bool async)
        {
            PlayerPrefs.SetString(filename, System.Convert.ToBase64String(data));
            return UniTask.CompletedTask;
        }

        public UniTask<LoadResult<byte[]>> LoadBytes(string filename, bool async)
        {
            if (!PlayerPrefs.HasKey(filename))
            {
                return new UniTask<LoadResult<byte[]>>(LoadResult<byte[]>.CreateNotFound());
            }
            var value = PlayerPrefs.GetString(filename);
            return new UniTask<LoadResult<byte[]>>(new LoadResult<byte[]>(System.Convert.FromBase64String(value)));
        }
    }

    public class JsonResolver : ISerializeResolver
    {
        readonly string password;
        readonly byte[] salt;
        readonly int iterations;

        public JsonResolver()
        {
        }

        public JsonResolver(string password, byte[] salt, int iterations)
        {
            this.password = password;
            this.salt = salt;
            this.iterations = iterations;
        }

        public byte[] Serialize<T>(T obj)
        {
            var shouldCrypt = password != null;
            var json = JsonUtility.ToJson(obj, prettyPrint: !shouldCrypt);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            if (shouldCrypt)
            {
                bytes = CryptUtil.Encrypt(bytes, password, salt, iterations);
            }
            return bytes;
        }

        public T Deserialize<T>(byte[] bytes)
        {
            if (password != null)
            {
                bytes = CryptUtil.Decrypt(bytes, password, salt, iterations);
            }
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
