using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace RNC.Tools.CertificateManager
{
    public static class CertificateManager
    {
        //Returns a certificate by searching through all likely places
        public static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            X509Certificate2 certificate;
            //foreach likely certificate store name
            var stores = new[] { StoreName.My, StoreName.Root };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                stores = new[] { StoreName.Root };
            }
            foreach (var name in stores)
            {
                //foreach store location
                foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
                {
                    //see if the certificate is in this store name and location
                    certificate = FindThumbprintInStore(thumbprint, name, location);
                    if (certificate != null)
                    {
                        //return the resulting certificate
                        return certificate;
                    }
                }
            }
            //certificate was not found
            throw new Exception(string.Format("The certificate with thumbprint {0} was not found",
                                               thumbprint));
        }

        public static X509Certificate2 FindThumbprintInStore(string thumbprint, StoreName name, StoreLocation location)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                name != StoreName.CertificateAuthority &&
                name != StoreName.Root)
            {
                return null;
            }

            //creates the store based on the input name and location e.g. name=My
            var certStore = new X509Store(name, location);
            certStore.Open(OpenFlags.ReadOnly);
            //finds the certificate in question in this store
            var certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint,
                                                             thumbprint, false);
            certStore.Close();

            if (certCollection.Count > 0)
            {
                //if it is found return
                return certCollection[0];
            }
            else
            {
                //if the certificate was not found return null
                return null;
            }
        }
    }
}
