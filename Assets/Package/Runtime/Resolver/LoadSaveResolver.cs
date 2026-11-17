#nullable enable
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TSKT.Files
{
    public interface ILoadSaveResolver
    {
        bool AnyExist(params string[] filenames);
        Awaitable<LoadResult<byte[]>> LoadBytesAsync(string filename);
        Awaitable SaveBytesAsync(string filename, byte[] data);
        LoadResult<byte[]> LoadBytes(string filename);
        void SaveBytes(string filename, ReadOnlySpan<byte> data);
    }

    public class FileResolver : ILoadSaveResolver
    {
        public enum UserFolder
        {
            ApplicationDirectory,
            MyDocuments,
            AppDataLocalLow,
        }

        public readonly string directory;

        public FileResolver(UserFolder type)
        {
            directory = type switch
            {
                UserFolder.AppDataLocalLow => UserFolderUtil.AppDataLocalLowCompanyProduct,
                UserFolder.MyDocuments => UserFolderUtil.MyDocumentsCompanyProduct,
                UserFolder.ApplicationDirectory => UserFolderUtil.GetApplicationDirectory(null),
                _ => throw new System.NotImplementedException(),
            };
        }
        public FileResolver(string? directory, bool userFolder = false)
        {
            var dir = userFolder ? Application.persistentDataPath : UserFolderUtil.GetApplicationDirectory(null);

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

        public async Awaitable SaveBytesAsync(string filename, byte[] data)
        {
            Directory.CreateDirectory(directory);
            var fullPath = GetPath(filename);
            var tempPath = GetTempPath(fullPath);
            try
            {
                await System.IO.File.WriteAllBytesAsync(tempPath, data);
                Commit(tempPath, fullPath);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }

        public void SaveBytes(string filename, ReadOnlySpan<byte> data)
        {
            Directory.CreateDirectory(directory);
            var fullPath = GetPath(filename);
            var tempPath = GetTempPath(fullPath);
            try
            {
                // File.WriteAllBytesだと配列へのコピーが要るので、ストリームに直接書く
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(data);
                }
                Commit(tempPath, fullPath);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }

        static string GetTempPath(string fullPath)
        {
            return fullPath + ".tmp";
        }

        /// <summary>
        /// 一時ファイルを本番のファイルに差し替える。書き込み途中で落ちても元のファイルが壊れないようにするため。
        /// </summary>
        static void Commit(string tempPath, string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                File.Move(tempPath, fullPath);
                return;
            }
            try
            {
                File.Replace(tempPath, fullPath, null);
            }
            catch (PlatformNotSupportedException)
            {
                // Replaceが使えない環境では消してから移動する。この間だけはアトミックではない。
                File.Delete(fullPath);
                File.Move(tempPath, fullPath);
            }
        }

        static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                // 後始末の失敗で元の例外を潰さない
            }
        }

        public async Awaitable<LoadResult<byte[]>> LoadBytesAsync(string filename)
        {
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

        public async Awaitable SaveBytesAsync(string filename, byte[] data)
        {
            SaveBytes(filename, data);
        }
        public void SaveBytes(string filename, ReadOnlySpan<byte> data)
        {
            PlayerPrefs.SetString(filename, System.Convert.ToBase64String(data));
        }

        public async Awaitable<LoadResult<byte[]>> LoadBytesAsync(string filename)
        {
            return LoadBytes(filename);
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
