using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Text;

namespace TSKT.Tests
{
    public class SignatureUtil
    {
        [Test]
        public void Verify()
        {
            var (publicKey, privateKey) = TSKT.SignatureUtil.GenerateKeys();
            var signature = TSKT.SignatureUtil.CreateDigitalSignature("hoge", privateKey);
            Assert.IsTrue(TSKT.SignatureUtil.VerifyDigitalSignature("hoge", signature, publicKey));
            Assert.IsFalse(TSKT.SignatureUtil.VerifyDigitalSignature("fuga", signature, publicKey));
        }
    }
}

