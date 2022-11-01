#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace TSKT.Files
{
    public class UserFolderUtil
    {
        public static string MyDocumentsCompanyProduct
        {
            get
            {
                try
                {
                    var myDocuments = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                    if (myDocuments == "")
                    {
                        return Application.persistentDataPath;
                    }
                    return Path.Combine(myDocuments, Application.companyName, Application.productName);
                }
                catch (System.PlatformNotSupportedException)
                {
                    return Application.persistentDataPath;
                }
            }
        }
        public static string AppDataLoalLowCompanyProduct => Application.persistentDataPath;
        public static string GetApplicationDirectory(string? subFolder)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var dir = Path.GetDirectoryName(Application.dataPath);
#else
            var dir =  Application.persistentDataPath;
#endif
            if (string.IsNullOrEmpty(subFolder))
            {
                return dir;
            }
            else
            {
                return Path.Combine(dir, subFolder);
            }
        }
    }
}
