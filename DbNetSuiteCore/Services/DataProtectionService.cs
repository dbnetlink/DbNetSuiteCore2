using DbNetSuiteCore.Helpers;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace DbNetSuiteCore.Services
{
    public class DataProtectionService
    {
        private readonly IDataProtector _protector;

        public DataProtectionService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            _protector = dataProtectionProvider.CreateProtector("DbNetSuiteCore");
        }

        public string Encrypt(string plaintext)
        {
            return _protector.Protect(plaintext);
        }

        public string Decrypt(string ciphertext)
        {
            try
            {
                return _protector.Unprotect(ciphertext);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}




