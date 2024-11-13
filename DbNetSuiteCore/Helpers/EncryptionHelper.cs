using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DbNetSuiteCore.Helpers
{
    public static class EncryptionHelper
    {
        private static byte[] DeriveKey(string key, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(salt), 100000))
            {
                return pbkdf2.GetBytes(32); // 256 bits for AES-256
            }
        }

        private static string CalculateChecksum(string data)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }

        public static string Encrypt(string jsonState, string key, string salt)
        {
            if (string.IsNullOrEmpty(jsonState))
                throw new ArgumentNullException(nameof(jsonState));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            // Add checksum to the original data
            var checksum = CalculateChecksum(jsonState);
            var dataWithChecksum = $"{checksum}|{jsonState}";

            using (var aes = Aes.Create())
            {
                var keyBytes = DeriveKey(key, salt);
                aes.Key = keyBytes;
                aes.GenerateIV();

                using (var msEncrypt = new MemoryStream())
                {
                    // Write the IV to the start of the stream
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(msEncrypt,
                        aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(dataWithChecksum);
                    }

                    var encrypted = msEncrypt.ToArray();
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        public static string Decrypt(string encryptedState, string key, string salt)
        {
            if (string.IsNullOrEmpty(encryptedState))
                throw new ArgumentNullException(nameof(encryptedState));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            var encryptedBytes = Convert.FromBase64String(encryptedState);

            using (var aes = Aes.Create())
            {
                var keyBytes = DeriveKey(key, salt);
                aes.Key = keyBytes;

                using (var msDecrypt = new MemoryStream(encryptedBytes))
                {
                    // Read the IV from the start of the stream
                    var iv = new byte[aes.IV.Length];
                    msDecrypt.Read(iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var cryptoStream = new CryptoStream(msDecrypt,
                        aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        var decryptedData = reader.ReadToEnd();
                        var parts = decryptedData.Split('|', 2);

                        if (parts.Length != 2)
                            throw new CryptographicException("Invalid data format");

                        var storedChecksum = parts[0];
                        var jsonState = parts[1];

                        // Verify checksum
                        var calculatedChecksum = CalculateChecksum(jsonState);
                        if (storedChecksum != calculatedChecksum)
                            throw new CryptographicException("Checksum validation failed");

                        return jsonState;
                    }
                }
            }
        }
    }
}