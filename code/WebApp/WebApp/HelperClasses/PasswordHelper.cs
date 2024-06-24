using System;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace WebApp.HelperClasses
{
    public static class PasswordHelper
    {
        public static (string Hash, string Salt) HashPassword(string password)
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            byte[] passwordAsBytes = System.Text.Encoding.UTF8.GetBytes(password);

            var argon2 = new Argon2id(passwordAsBytes)
            {
                Salt = saltBytes,
                DegreeOfParallelism = 8, // number of threads to use
                Iterations = 4,
                MemorySize = 1024 * 64 // 64 MB
            };
            byte[] hashBytes = argon2.GetBytes(32);
            string hash = Convert.ToBase64String(hashBytes);

            return (hash, salt);
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] enteredPasswordAsBytes = System.Text.Encoding.UTF8.GetBytes(enteredPassword);

            var argon2 = new Argon2id(enteredPasswordAsBytes)
            {
                Salt = saltBytes,
                DegreeOfParallelism = 8,
                Iterations = 4,
                MemorySize = 1024 * 64
            };
            byte[] enteredHashBytes = argon2.GetBytes(32);
            string enteredHash = Convert.ToBase64String(enteredHashBytes);

            return enteredHash == storedHash;
        }
    }
}
