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
        public static string AppDirectory
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                return Path.GetDirectoryName(Application.dataPath);
#else
                return Application.persistentDataPath;
#endif
            }
        }
    }
}