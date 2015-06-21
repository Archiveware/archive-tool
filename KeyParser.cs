using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using System.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.X509;

namespace ArchiveTool
{
    class KeyParser
    {
        public AsymmetricCipherKeyPair KeyPair;
        public string SubjectKeyIdentifier;

        public KeyParser(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".pfx")
            {
                Console.Write("PFX password: ");
                X509Certificate2Collection certs = new X509Certificate2Collection();
                try
                {
                    certs.Import(path, GetPassword(), X509KeyStorageFlags.Exportable);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                foreach (X509Certificate2 cert in certs)
                {
                    if (cert.HasPrivateKey)
                    {
                        Console.WriteLine("Using private key from certificate '{0}'", cert.Subject);
                        KeyPair = DotNetUtilities.GetKeyPair(cert.PrivateKey);
                    }
                }
            }
            else
                try
                {
                    using (var reader = File.OpenText(path))
                    {
                        KeyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            if (KeyPair == null)
            {
                Console.WriteLine("WARNING: Unable to obtain private key from {0}. This should be a password-protected PFX file or password-less PEM file.", path);
                Environment.Exit(1);
            }
            else
            {
                SubjectKeyIdentifier = string.Join(string.Empty, 
                    Array.ConvertAll(new SubjectKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(KeyPair.Public)).GetKeyIdentifier(), 
                    b => b.ToString("x2")));
                Console.WriteLine("Loaded private key with ID {0} from {1}", SubjectKeyIdentifier, path);
            }
        }

        internal string GetPassword()
        {
            var password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                else
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (password.Length > 0)
                        {
                            password = password.Remove(password.Length - 1, 1);
                            Console.Write("\b \b");
                        }
                    }
                    else if ((int)key.Key >= 32)
                    {
                        password.Append(key.KeyChar);
                        Console.Write("*");
                    }
            }
            Console.WriteLine();
            return password.ToString();
        }
    }
}
