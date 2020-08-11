using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TSKT.Files
{
    public interface ILoadSaveResolver
    {
        bool AnyExist(params string[] filenames);
        UniTask<LoadResult<byte[]>> LoadBytesAsync(string filename);
        UniTask SaveBytesAsync(string filename, byte[] data);
        LoadResult<byte[]> LoadBytes(string filename);
        void SaveBytes(string filename, byte[] data);
    }

    public interface ISerializeResolver
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] bytes);
    }

    public class DefaultResolver : ILoadSaveResolver
    {
        static readonly Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>();
        static int processCount = 0;

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
                var path = GetPath(filename);
                if (cache.TryGetValue(path, out var file))
                {
                    if (file != null)
                    {
                        return true;
                    }
                }
                if (System.IO.File.Exists(path))
                {
                    return true;
                }
                cache[path] = null;
            }
            return false;
        }

        public async UniTask SaveBytesAsync(string filename, byte[] data)
        {
#if UNITY_WEBGL
            SaveBytes(filename, data);
            return;
#endif
            await UniTask.WaitWhile(() => processCount > 0);
            ++processCount;
            try
            {
                var fullPath = GetPath(filename);
                cache[fullPath] = data;
                CreateDictionary(fullPath);

                using (var file = System.IO.File.Open(fullPath, FileMode.Create))
                {
                    await file.WriteAsync(data, 0, data.Length).AsUniTask();
                }
            }
            finally
            {
                --processCount;
            }
        }

        public void SaveBytes(string filename, byte[] data)
        {
            var fullPath = GetPath(filename);
            CreateDictionary(fullPath);
            System.IO.File.WriteAllBytes(fullPath, data);

            cache[fullPath] = data;
        }

        public async UniTask<LoadResult<byte[]>> LoadBytesAsync(string filename)
        {
#if UNITY_WEBGL
            return LoadBytes(filename);
#endif
            await UniTask.WaitWhile(() => processCount > 0);
            ++processCount;
            try
            {
                var fullPath = GetPath(filename);

                if (cache.TryGetValue(fullPath, out var cachedFile))
                {
                    if (cachedFile == null)
                    {
                        return LoadResult<byte[]>.CreateNotFound();
                    }
                    return new LoadResult<byte[]>(cachedFile);
                }

                try
                {
                    using (var fileStream = System.IO.File.OpenRead(fullPath))
                    {
                        var bytes = new byte[fileStream.Length];
                        await fileStream.ReadAsync(bytes, 0, bytes.Length).AsUniTask();
                        cache[fullPath] = bytes;
                        --processCount;
                        return new LoadResult<byte[]>(bytes);
                    }
                }
                catch (DirectoryNotFoundException ex)
                {
                    cache[fullPath] = null;
                    return LoadResult<byte[]>.CreateNotFound(ex);
                }
                catch (FileNotFoundException ex)
                {
                    cache[fullPath] = null;
                    return LoadResult<byte[]>.CreateNotFound(ex);
                }
            }
            finally
            {
                --processCount;
            }
        }

        public LoadResult<byte[]> LoadBytes(string filename)
        {
            var fullPath = GetPath(filename);

            if (cache.TryGetValue(fullPath, out var cachedFile))
            {
                if (cachedFile == null)
                {
                    return LoadResult<byte[]>.CreateNotFound();
                }
                return new LoadResult<byte[]>(cachedFile);
            }

            try
            {
                var bytes = System.IO.File.ReadAllBytes(fullPath);
                cache[fullPath] = bytes;
                return new LoadResult<byte[]>(bytes);
            }
            catch (DirectoryNotFoundException ex)
            {
                cache[fullPath] = null;
                return LoadResult<byte[]>.CreateNotFound(ex);
            }
            catch (FileNotFoundException ex)
            {
                cache[fullPath] = null;
                return LoadResult<byte[]>.CreateNotFound(ex);
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

        public UniTask SaveBytesAsync(string filename, byte[] data)
        {
            SaveBytes(filename, data);
            return UniTask.CompletedTask;
        }
        public void SaveBytes(string filename, byte[] data)
        {
            PlayerPrefs.SetString(filename, System.Convert.ToBase64String(data));
        }

        public UniTask<LoadResult<byte[]>> LoadBytesAsync(string filename)
        {
            var result = LoadBytes(filename);
            return UniTask.FromResult(result);
        }

        public LoadResult<byte[]> LoadBytes(string filename)
        {
            if (!PlayerPrefs.HasKey(filename))
            {
                return LoadResult<byte[]>.CreateNotFound();
            }
            var value = PlayerPrefs.GetString(filename);
            return new LoadResult<byte[]>(System.Convert.FromBase64String(value));
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
