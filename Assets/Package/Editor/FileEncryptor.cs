#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if TSKT_EDITOR_SUPPORT
namespace TSKT
{
    public class FileEncryptor : EditorWindow
    {
        string password;
        string salt;
        int iterations = 1000;
        bool compress = false;

        [MenuItem("TSKT/File Encryptor")]
        static void Init()
        {
            var window = (FileEncryptor)GetWindow(typeof(FileEncryptor));
            window.Show();
        }

        void OnGUI()
        {
            password = EditorGUILayout.TextField("password", password);
            salt = EditorGUILayout.TextField("salt", salt);
            iterations = EditorGUILayout.IntField("iterations", iterations);
            compress = EditorGUILayout.ToggleLeft("compress", compress);

            if (GUILayout.Button("Encrypt"))
            {
                if (!Selection.activeObject)
                {
                    return;
                }

                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                var bytes = System.IO.File.ReadAllBytes(path);
                var newPath = EditorUtil.GenerateUniqueAssetPath(Selection.activeObject, "bytes");

                var saltBytes = System.Text.Encoding.UTF8.GetBytes(salt);
                byte[] encryptedBytes;
                if (compress)
                {
                    encryptedBytes = CryptUtil.Encrypt(CompressUtil.Compress(bytes), password, saltBytes, iterations).ToArray();
                    var t = CompressUtil.Decompress(CryptUtil.Decrypt(encryptedBytes, password, saltBytes, iterations));
                    if (!bytes.SequenceEqual(t))
                    {
                        Debug.LogError("error : can't decrypt");
                        return;
                    }
                }
                else
                {
                    encryptedBytes = CryptUtil.Encrypt(bytes, password, saltBytes, iterations).ToArray();
                    var t = CryptUtil.Decrypt(encryptedBytes, password, saltBytes, iterations);
                    if (!bytes.SequenceEqual(t))
                    {
                        Debug.LogError("error : can't decrypt");
                        return;
                    }
                }
                System.IO.File.WriteAllBytes(newPath, encryptedBytes);
                AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
                Debug.Log("saved : " + newPath);
                Debug.Log("password : " + password);
                Debug.Log("salt : " + salt);
                Debug.Log("iterations : " + iterations.ToString());
                Debug.Log("compress : " + compress.ToString());
            }
        }
    }
}
#endif
