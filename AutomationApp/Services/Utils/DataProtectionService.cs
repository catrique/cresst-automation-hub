using System;
using System.Text;
using System.Security.Cryptography;

namespace AutomationApp.Services.Utils
{
    public static class DataProtectionService
    {
        private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("CresstAutomationSalt_9876");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, OptionalEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to encrypt data using DPAPI.", ex);
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if(string.IsNullOrWhiteSpace(encryptedText))
            return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, OptionalEntropy, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plainBytes);
                
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to decrypt data using DPAPI. Ensure you are on the same machine and user account.", ex);
            }
        }


    }
    
}