#nullable enable
using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Text;
using TSKT.Files;
using System.Linq;
using System.Buffers;

namespace TSKT.Tests
{
    public class JsonResolver
    {
        [Test]
        public void Serialize()
        {
            var from = Color.cyan;
            var resolver = new TSKT.Files.JsonResolver("erfaowkfga@oper", Encoding.UTF8.GetBytes("ganieu:vjjoejf"), 1000);
            var writer = new ArrayBufferWriter<byte>();
            resolver.Serialize(from, writer);
            var bytes = writer.WrittenSpan.ToArray();
            var result = resolver.Deserialize<Color>(bytes);
            Assert.AreEqual(from, result);
        }
    }
}

