using System;
using System.Security.Cryptography;

namespace WebApp.HelperClasses
{
    public static class PasswordHelper
    {
        public static (string Hash, string Salt) HashPassword(string password)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[16];
                rng.GetBytes(saltBytes);
                string salt = Convert.ToBase64String(saltBytes);

                var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000);
                byte[] hashBytes = pbkdf2.GetBytes(20);
                string hash = Convert.ToBase64String(hashBytes);

                return (hash, salt);
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, saltBytes, 10000);
            byte[] enteredHashBytes = pbkdf2.GetBytes(20);
            string enteredHash = Convert.ToBase64String(enteredHashBytes);

            return enteredHash == storedHash;
        }
    }
}
