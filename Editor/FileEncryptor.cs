using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TSKT
{
    public class FileEncryptor : EditorWindow
    {
        string password;
        string salt;
        int iterations = 1000;
        bool compress = false;

        [MenuItem("TSKT/File Ecryptor")]
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

                var bytes = File.ReadAllBytes(path);
                var newPath = EditorUtil.GenerateUniqueAssetPath(Selection.activeObject, "bytes");

                var saltBytes = System.Text.Encoding.UTF8.GetBytes(salt);
                byte[] encryptedBytes;
                if (compress)
                {
                    encryptedBytes = CryptUtil.Encrypt(bytes, password, saltBytes, iterations);
                    var t = CryptUtil.Decrypt(encryptedBytes, password, saltBytes, iterations);
                    if (!bytes.SequenceEqual(t))
                    {
                        Debug.LogError("error : can't decrypt");
                        return;
                    }
                }
                else
                {
                    using (var rijndael = CryptUtil.CreateRijndael(password, saltBytes, iterations))
                    {
                        using (var encryptor = rijndael.CreateEncryptor())
                        {
                            encryptedBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

                            using (var decryptor = rijndael.CreateDecryptor())
                            {
                                var t = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                                if (!bytes.SequenceEqual(t))
                                {
                                    Debug.LogError("error : can't decrypt");
                                    return;
                                }
                            }
                        }
                    }
                }
                File.WriteAllBytes(newPath, encryptedBytes);
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