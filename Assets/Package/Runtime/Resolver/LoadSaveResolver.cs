#nullable enable
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

    public class DefaultResolver : ILoadSaveResolver
    {
        static int processCount = 0;

        public readonly string? directory;

        public DefaultResolver(string? directory, bool userFolder = false)
        {
            var dir = userFolder ? Application.persistentDataPath : File.AppDirectory;

            if (directory == null)
            {
                this.directory = dir;
            }
            else
            {
                this.directory = Path.Combine(dir, directory);
            }
        }

        string GetPath(string filename)
        {
            if (directory == null)
            {
                return filename;
            }
            return Path.Combine(directory, filename);
        }

        public bool AnyExist(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                var path = GetPath(filename);
                if (System.IO.File.Exists(path))
                {
                    return true;
                }
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

                try
                {
                    using (var fileStream = System.IO.File.OpenRead(fullPath))
                    {
                        var bytes = new byte[fileStream.Length];
                        await fileStream.ReadAsync(bytes, 0, bytes.Length).AsUniTask();
                        return new LoadResult<byte[]>(bytes);
                    }
                }
                catch (DirectoryNotFoundException ex)
                {
                    return LoadResult<byte[]>.CreateNotFound(ex);
                }
                catch (FileNotFoundException ex)
                {
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

            try
            {
                var bytes = System.IO.File.ReadAllBytes(fullPath);
                return new LoadResult<byte[]>(bytes);
            }
            catch (DirectoryNotFoundException ex)
            {
                return LoadResult<byte[]>.CreateNotFound(ex);
            }
            catch (FileNotFoundException ex)
            {
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

}
