using UnityEngine;
using System.Collections;

namespace TSKT
{
    public static class SignatureUtil
    {
        public static (string publicKey, string privateKey) GenerateKeys()
        {
            using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
            {
                var publicKey = rsa.ToXmlString(false);
                var privateKey = rsa.ToXmlString(true);
                return (publicKey, privateKey);
            }
        }

        public static byte[] CreateDigitalSignature(string message, string privateKey)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            return CreateDigitalSignature(bytes, privateKey);
        }

        public static byte[] CreateDigitalSignature(byte[] bytes, string privateKey)
        {
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                var hashData = sha.ComputeHash(bytes);

                using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privateKey);

                    var rsaFormatter = new System.Security.Cryptography.RSAPKCS1SignatureFormatter(rsa);
                    rsaFormatter.SetHashAlgorithm("SHA256");

                    var signedValue = rsaFormatter.CreateSignature(hashData);
                    return signedValue;
                }
            }
        }

        public static bool VerifyDigitalSignature(string message, byte[] signature, string publicKey)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            return VerifyDigitalSignature(bytes, signature, publicKey);
        }

        public static bool VerifyDigitalSignature(byte[] bytes, byte[] signature, string publicKey)
        {
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                var hashData = sha.ComputeHash(bytes);

                using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(publicKey);

                    var rsaDeformatter = new System.Security.Cryptography.RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm("SHA256");

                    return rsaDeformatter.VerifySignature(hashData, signature);
                }
            }
        }
    }
}
