using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Cms;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;

namespace ArchiveTool
{
    class ArchiveSetKeys
    {
        Dictionary<UInt16, byte[]> Keys = new Dictionary<UInt16, byte[]>();

        public KeyParser ExplicitKey;

        public bool GetKeyByIndex(int index, out byte[] key)
        {
            if (index == 0)
            {
                key = new byte[32];
                return true;
            }

            if (Keys.ContainsKey((UInt16)index))
            {
                key = Keys[(UInt16)index];
                return true;
            }

            key = null;
            return false;
        }

        internal void AddFromPkcs7Message(byte[] blob)
        {
            var parser = new CmsEnvelopedDataParser(blob);

            var enumerator = parser.GetRecipientInfos().GetRecipients().GetEnumerator();
            if (enumerator.MoveNext())
            {
                var recipient = (KeyTransRecipientInformation)enumerator.Current;
                var subjectKeyIdentifier = string.Join(string.Empty, Array.ConvertAll(recipient.RecipientID.SubjectKeyIdentifier, b => b.ToString("x2"))).Substring(4);
                
                AsymmetricCipherKeyPair keyPair = null;
                if (ExplicitKey != null && ExplicitKey.SubjectKeyIdentifier != null && ExplicitKey.SubjectKeyIdentifier.Equals(subjectKeyIdentifier))
                    keyPair = ExplicitKey.KeyPair;

                var myCert = MatchingCertFromLocalStore(recipient.RecipientID.SubjectKeyIdentifier);
                if (myCert != null || keyPair !=null)
                {
                    if (keyPair == null)
                        keyPair = DotNetUtilities.GetRsaKeyPair((System.Security.Cryptography.RSA)myCert.PrivateKey);
                    
                    byte[] keySet;
                    try
                    {
                        keySet = recipient.GetContent(keyPair.Private);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unexpected error: unable to decrypt embedded PKCS#7 message (containing archive set keys): {0}", ex.Message);
                        return;
                    }

                    UInt16 keyIndex = 1;
                    for (int i = 0; i < keySet.Length; i += 32)
                    {
                        if (!Keys.ContainsKey(keyIndex))
                        {
                            byte[] key = new byte[32];
                            Array.ConstrainedCopy(keySet, i, key, 0, 32);
                            Keys.Add(keyIndex, key);
                            keyIndex++;
                        }
                    }
                }
            }
        }

        internal static X509Certificate2 MatchingCertFromLocalStore(byte[] subjectKeyIdentifier)
        {
            const string SubjectKeyIdentifierExtensionOid = "2.5.29.14";

            var store = new X509Store("MY");
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                if (cert.HasPrivateKey && cert.Extensions[SubjectKeyIdentifierExtensionOid] != null && cert.Extensions[SubjectKeyIdentifierExtensionOid].RawData.SequenceEqual(subjectKeyIdentifier))
                {
                    store.Close();
                    return cert;
                }
            }

            store.Close();
            return null;
        }
    }
}
