#nullable enable
using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Text;
using TSKT.Files;
using System.Linq;

namespace TSKT.Tests
{
    public class JsonResolver
    {
        [Test]
        public void Serialize()
        {
            var from = Color.cyan;
            var resolver = new TSKT.Files.JsonResolver("erfaowkfga@oper", Encoding.UTF8.GetBytes("ganieu:vjjoejf"), 1000);
            var bytes = resolver.Serialize(from);
            var result = resolver.Deserialize<Color>(bytes);
            Assert.AreEqual(from, result);
        }
    }
}

