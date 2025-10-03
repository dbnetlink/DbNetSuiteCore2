using DbNetSuiteCore.Helpers;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace DbNetSuiteCore.Services
{
    public class DataProtectionService
    {
        // The IDataProtector is created with a unique purpose string.
        private readonly IDataProtector _protector;

        // 1. Inject the IDataProtectionProvider
        public DataProtectionService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            var encryptionConfig = TextHelper.GetEncryptionConfig(configuration);
            _protector = dataProtectionProvider.CreateProtector(encryptionConfig.DataProtectionPurpose);
        }

        // Encrypt (Protect) the plain text
        public string Encrypt(string plaintext)
        {
            return _protector.Protect(plaintext);
        }

        // Decrypt (Unprotect) the ciphertext
        public string Decrypt(string ciphertext)
        {
            try
            {
                return _protector.Unprotect(ciphertext);
            }
            catch (CryptographicException)
            {
                // Handle cases where the data is invalid, tampered with, or the key has expired.
                // For example, return an empty string or null.
                return null;
            }
        }
    }
}




