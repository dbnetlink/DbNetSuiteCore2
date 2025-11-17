using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Provides secure AES-256 encryption and decryption using PBKDF2 for key derivation.
/// </summary>
/// 
namespace DbNetSuiteCore.Helpers
{
    public static class AesEncryptor
    {
        // --- Configuration ---
        // Key size in bits. We use 256-bit for strong security.
        private const int KeySize = 256;

        // Block size in bits. AES standard is 128-bit.
        private const int BlockSize = 128;

        // Number of iterations for PBKDF2.
        // A higher number increases security but slows down the process.
        // 100,000 is a good modern baseline (as of 2024).
        private const int DerivationIterations = 100_000;

        // Salt size in bytes. 16 bytes (128 bits) is recommended.
        private const int SaltSize = 16;

        /// <summary>
        /// Encrypts a plain-text string using a password.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <param name="password">The password to use for encryption.</param>
        /// <returns>A Base64-encoded string representing the [Salt]+[IV]+[Ciphertext].</returns>
        /// <exception cref="ArgumentNullException">Thrown if plainText or password is null or empty.</exception>
        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            // 1. Generate a new random Salt and IV for each encryption
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] iv = RandomNumberGenerator.GetBytes(BlockSize / 8); // 128 bits / 8 = 16 bytes

            // 2. Derive the key from the password and salt
            // We use Rfc2898DeriveBytes which implements PBKDF2
            using (var kdf = new Rfc2898DeriveBytes(password, salt, DerivationIterations, HashAlgorithmName.SHA256))
            {
                byte[] key = kdf.GetBytes(KeySize / 8); // 256 bits / 8 = 32 bytes

                // 3. Create the AES algorithm instance
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC; // Cipher Block Chaining mode
                    aes.Key = key;
                    aes.IV = iv;

                    // 4. Create the encryptor
                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // 5. Prepend the Salt and IV to the stream
                        // [Salt (16 bytes)] + [IV (16 bytes)] + [Ciphertext (...)]
                        ms.Write(salt, 0, salt.Length);
                        ms.Write(iv, 0, iv.Length);

                        // 6. Encrypt the data
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                            cs.Write(plainTextBytes, 0, plainTextBytes.Length);
                        } // cs.Dispose() will flush the final block

                        // 7. Return the combined data as a Base64 string
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext string using a password.
        /// </summary>
        /// <param name="cipherText">The Base64-encoded text to decrypt ([Salt]+[IV]+[Ciphertext]).</param>
        /// <param name="password">The password used for the original encryption.</param>
        /// <returns>The decrypted plain-text string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if cipherText or password is null or empty.</exception>
        /// <exception cref="CryptographicException">Thrown if decryption fails (e.g., wrong password, corrupt data).</exception>
        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            // 1. Decode the Base64 string
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            // 2. Extract the Salt and IV from the beginning of the data
            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[BlockSize / 8]; // 16 bytes

            // Check if the cipherText is long enough
            if (fullCipher.Length < SaltSize + iv.Length)
                throw new CryptographicException("Invalid ciphertext. Data is too short.");

            Buffer.BlockCopy(fullCipher, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(fullCipher, SaltSize, iv, 0, iv.Length);

            // 3. Derive the key *using the extracted salt*
            using (var kdf = new Rfc2898DeriveBytes(password, salt, DerivationIterations, HashAlgorithmName.SHA256))
            {
                byte[] key = kdf.GetBytes(KeySize / 8);

                // 4. Create the AES algorithm instance
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = key;
                    aes.IV = iv; // Use the extracted IV

                    // 5. Create the decryptor
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream())
                    {
                        // 6. Write the *ciphertext part* of the data to the memory stream
                        // The ciphertext starts *after* the Salt and IV
                        int cipherDataLength = fullCipher.Length - SaltSize - iv.Length;
                        ms.Write(fullCipher, SaltSize + iv.Length, cipherDataLength);
                        ms.Position = 0; // Rewind stream to the beginning

                        // 7. Decrypt the data
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        using (var sr = new StreamReader(cs, Encoding.UTF8))
                        {
                            // A CryptographicException will be thrown here if the password is wrong
                            // or the data is corrupted.
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}