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

        public NotNullFile(string filename, FileIO io)
        {
            this.filename = filename;
            this.io = io;
        }
        public T Value
        {
            get
            {
                if (value == null)
                {
                    var result = io.Load<T>(filename);
                    if (result.Succeeded)
                    {
                        value = result.value;
                    }
                    else
                    {
                        value = new T();
                    }
                }
                return value;
            }
            set
            {
                this.value = value;
            }
        }

        public async Awaitable<T> LoadAsync(System.IProgress<float>? progress = null)
        {
            if (value != null)
            {
                progress?.Report(1f);
                return value;
            }
            var result = await io.LoadAsync<T>(filename, progress);
            if (value != null)
            {
                return value;
            }
            if (result.Succeeded)
            {
                value = result.value;
            }
            else
            {
                value = new T();
            }
            return value;
        }

        public void Save()
        {
            if (value != null)
            {
                io.Save(filename, value);
            }
        }
        public async Awaitable SaveAsync(System.IProgress<float>? progress = null)
        {
            if (value == null)
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
