using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;
using UnityEngine.UI;

namespace TSKT
{
    public class FileIOSample : MonoBehaviour
    {
        [System.Serializable]
        public class Udon
        {
            public string name;
            public bool men;
            public bool chikuwaTen;
            public bool kashiwaTen;
            public bool tenkasu;
        }

        [SerializeField]
        Toggle usePlayerPrefs = default;

        File CreateFileIO()
        {
            Files.ILoadSaveResolver ioResolver;
            if (usePlayerPrefs.isOn)
            {
                ioResolver = new Files.PrefsResolver();
            }
            else
            {
                ioResolver = new Files.DefaultResolver(directory: "SaveData");
            }

            var serializeResolver = new Files.JsonResolver(
                password: "56562",
                salt: System.Text.Encoding.UTF8.GetBytes("ごろごろにゃーちゃん"),
                iterations: 1000);

            return new File(ioResolver, serializeResolver);
        }

        public async void SaveStringAsync()
        {
            await CreateFileIO().SaveStringAsync("foo.txt", "shami");
            Debug.Log("save foo.txt : shami");
        }

        public async void LoadStringAsync()
        {
            var text = await CreateFileIO().LoadStringAsync("foo.txt");
            Debug.Log("load foo.txt");
            Debug.Log(text.Succeeded ? text.value : text.state.ToString());
        }

        public void SaveString()
        {
            CreateFileIO().SaveString("foo.txt", "momo");
            Debug.Log("save foo.txt : momo");
        }

        public void LoadString()
        {
            var text = CreateFileIO().LoadString("foo.txt");
            Debug.Log("load foo.txt");
            Debug.Log(text.Succeeded ? text.value : text.state.ToString());
        }

        public async void SaveBytesAsync()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("mikan");
            await CreateFileIO().SaveBytesAsync("foo.txt", bytes);
            Debug.Log("save foo.txt : mikan");
        }

        public async void LoadBytesAsync()
        {
            var bytes = await CreateFileIO().LoadBytesAsync("foo.txt");
            Debug.Log("load foo.txt");
            Debug.Log(bytes.Succeeded ? System.Text.Encoding.UTF8.GetString(bytes.value) : bytes.state.ToString());
        }

        public void SaveBytes()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("gosenzo");
            CreateFileIO().SaveBytes("foo.txt", bytes);
            Debug.Log("save foo.txt : gosenzo");
        }

        public void LoadBytes()
        {
            var bytes = CreateFileIO().LoadBytes("foo.txt");
            Debug.Log("load foo.txt");
            Debug.Log(bytes.Succeeded ? System.Text.Encoding.UTF8.GetString(bytes.value) : bytes.state.ToString());
        }

        public void Save()
        {
            var udon = new Udon() { name = "chikuwa" };
            CreateFileIO().Save("udon.bytes", udon);
            Debug.Log("save udon.bytes : chikuwa");
        }

        public void Load()
        {
            var udon = CreateFileIO().Load<Udon>("udon.bytes");
            Debug.Log("load udon.bytes");
            Debug.Log(udon.Succeeded ? udon.value.name : udon.state.ToString());
        }

        public async void SaveAsync()
        {
            var udon = new Udon() { name = "kashiwa" };
            await CreateFileIO().SaveAsync("udon.bytes", udon);
            Debug.Log("save udon.bytes : kashiwa");
        }

        public async void LoadAsync()
        {
            var udon = await CreateFileIO().LoadAsync<Udon>("udon.bytes");
            Debug.Log("load udon.bytes");
            Debug.Log(udon.Succeeded ? udon.value.name : udon.state.ToString());
        }
    }
}
