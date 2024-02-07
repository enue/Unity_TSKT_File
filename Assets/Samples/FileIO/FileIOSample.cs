#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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

        FileIO CreateFileIO()
        {
            Files.ILoadSaveResolver ioResolver;
            if (usePlayerPrefs.isOn)
            {
                ioResolver = new Files.PrefsResolver();
            }
            else
            {
                ioResolver = new Files.FileResolver(directory: "SaveData");
            }

            var serializeResolver = new Files.JsonResolver(
                password: "56562",
                salt: System.Text.Encoding.UTF8.GetBytes("ごろごろにゃーちゃん"),
                iterations: 1000);

            return new FileIO(ioResolver, serializeResolver);
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
