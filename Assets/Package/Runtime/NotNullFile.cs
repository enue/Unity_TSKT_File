#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class NotNullFile<T>
        where T : new()
    {
        public readonly string filename;
        readonly FileIO io;
        T? value;
        // Tが構造体だとvalue == nullで未ロードを判定できないのでフラグで持つ
        bool loaded;

        public NotNullFile(string filename, FileIO io)
        {
            this.filename = filename;
            this.io = io;
        }
        public T Value
        {
            get
            {
                if (!loaded || value == null)
                {
                    value = Resolve(io.Load<T>(filename));
                    loaded = true;
                }
                return value;
            }
            set
            {
                this.value = value;
                loaded = true;
            }
        }

        public async Awaitable<T> LoadAsync(System.IProgress<float>? progress = null)
        {
            if (loaded && value != null)
            {
                progress?.Report(1f);
                return value;
            }
            var result = await io.LoadAsync<T>(filename, progress);
            if (loaded && value != null)
            {
                return value;
            }
            value = Resolve(result);
            loaded = true;
            return value;
        }

        T Resolve(LoadResult<T> result)
        {
            if (result.Succeeded)
            {
                return result.value;
            }
            return new T();
        }

        public void Save()
        {
            if (loaded && value != null)
            {
                io.Save(filename, value);
            }
        }
        public async Awaitable SaveAsync(System.IProgress<float>? progress = null)
        {
            if (!loaded || value == null)
            {
                progress?.Report(1f);
            }
            else
            {
                await io.SaveAsync(filename, value, progress);
            }
        }
    }
}
