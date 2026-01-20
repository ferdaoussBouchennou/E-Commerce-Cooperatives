using System;
using System.Security.Cryptography;
using System.Text;

namespace E_Commerce_Cooperatives.Models
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash un mot de passe en utilisant SHA256
        /// </summary>
        /// <param name="password">Le mot de passe en clair</param>
        /// <returns>Le hash du mot de passe</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(password));

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convertir le mot de passe en bytes
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convertir les bytes en string hexadécimale
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Vérifie si un mot de passe correspond à un hash
        /// </summary>
        /// <param name="password">Le mot de passe en clair</param>
        /// <param name="hashedPassword">Le hash du mot de passe stocké</param>
        /// <returns>True si le mot de passe correspond, False sinon</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            string hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Génère un token aléatoire pour la réinitialisation du mot de passe
        /// </summary>
        /// <returns>Un token aléatoire</returns>
        public static string GenerateResetToken()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }

        /// <summary>
        /// Génère un code de vérification à 6 chiffres
        /// </summary>
        /// <returns>Un code à 6 chiffres</returns>
        public static string GenerateVerificationCode()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int randomNumber = BitConverter.ToInt32(randomBytes, 0);
                // Générer un nombre entre 100000 et 999999
                int code = Math.Abs(randomNumber % 900000) + 100000;
                return code.ToString("D6");
            }
        }
    }
}

