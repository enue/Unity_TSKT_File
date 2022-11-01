#nullable enable
using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Text;
using TSKT.Files;

namespace TSKT.Tests
{
    public class UserFolderUtilTest
    {
        [Test]
        public void Paths()
        {
            var appData = UserFolderUtil.AppDataLoalLowCompanyProduct;
            var appPath = UserFolderUtil.GetApplicationDirectory("SaveData");
            var myDocument = UserFolderUtil.MyDocumentsCompanyProduct;
        }
    }
}
