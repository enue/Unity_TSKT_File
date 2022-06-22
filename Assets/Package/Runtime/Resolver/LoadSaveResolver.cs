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
        string[] GetFiles();
        UniTask<LoadResult<byte[]>> LoadBytesAsync(string filename);
        UniTask SaveBytesAsync(string filename, byte[] data);
        LoadResult<byte[]> LoadBytes(string filename);
        void SaveBytes(string filename, byte[] data);
    }

    public class FileResolver : ILoadSaveResolver
    {
        static int processCount = 0;

        public readonly string directory;

        public FileResolver(string? directory, bool userFolder = false)
        {
            var dir = userFolder ? Application.persistentDataPath : FileIO.AppDirectory;

            if (string.IsNullOrEmpty(directory))
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
            return Path.Combine(directory, filename);
        }

        public string[] GetFiles()
        {
            return Directory.GetFiles(directory);
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
            await UniTask.WaitWhile(() => processCount > 0);
            ++processCount;
            try
            {
                Directory.CreateDirectory(directory);

                var fullPath = GetPath(filename);
                await System.IO.File.WriteAllBytesAsync(fullPath, data);
            }
            finally
            {
                --processCount;
            }
        }

        public void SaveBytes(string filename, byte[] data)
        {
            Directory.CreateDirectory(directory);
            var fullPath = GetPath(filename);
            System.IO.File.WriteAllBytes(fullPath, data);
        }

        public async UniTask<LoadResult<byte[]>> LoadBytesAsync(string filename)
        {
            await UniTask.WaitWhile(() => processCount > 0);
            ++processCount;
            try
            {
                var fullPath = GetPath(filename);
                var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
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
            catch (IOException ex)
            {
                return LoadResult<byte[]>.CreateError(ex);
            }
            finally
            {
                --processCount;
            }
        }

        public LoadResult<byte[]> LoadBytes(string filename)
        {
            try
            {
                var fullPath = GetPath(filename);
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
            catch (IOException ex)
            {
                return LoadResult<byte[]>.CreateError(ex);
            }
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

        public string[] GetFiles()
        {
            throw new System.NotImplementedException();
        }
    }

}
