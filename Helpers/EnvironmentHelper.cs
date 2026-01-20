using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace E_Commerce_Cooperatives.Helpers
{
    /// <summary>
    /// Helper class to load and manage environment variables from .env file
    /// </summary>
    public static class EnvironmentHelper
    {
        private static Dictionary<string, string> _envVariables = new Dictionary<string, string>();
        private static bool _isLoaded = false;

        /// <summary>
        /// Load environment variables from .env file
        /// </summary>
        public static void LoadEnvironmentVariables()
        {
            if (_isLoaded)
                return;

            try
            {
                string envFilePath = HostingEnvironment.MapPath("~/.env");
                
                if (!File.Exists(envFilePath))
                {
                    throw new FileNotFoundException($"Le fichier .env est introuvable à l'emplacement : {envFilePath}. " +
                        "Veuillez créer un fichier .env en copiant .env.example et en ajoutant vos valeurs.");
                }

                var lines = File.ReadAllLines(envFilePath);
                
                foreach (var line in lines)
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        // Set both in dictionary and environment
                        _envVariables[key] = value;
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du chargement du fichier .env : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get an environment variable value
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Variable value or default value</returns>
        public static string GetVariable(string key, string defaultValue = null)
        {
            if (!_isLoaded)
                LoadEnvironmentVariables();

            if (_envVariables.ContainsKey(key))
                return _envVariables[key];

            return defaultValue;
        }

        /// <summary>
        /// Get a required environment variable (throws exception if not found)
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <returns>Variable value</returns>
        public static string GetRequiredVariable(string key)
        {
            var value = GetVariable(key);
            
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception($"La variable d'environnement requise '{key}' est manquante dans le fichier .env");
            }

            return value;
        }

        /// <summary>
        /// Get database connection string from environment variables
        /// </summary>
        /// <returns>Connection string</returns>
        public static string GetDatabaseConnectionString()
        {
            var server = GetRequiredVariable("DB_SERVER");
            var database = GetRequiredVariable("DB_NAME");
            var user = GetRequiredVariable("DB_USER");
            var password = GetRequiredVariable("DB_PASSWORD");

            return $"Server={server};Database={database};User Id={user};Password={password};Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;";
        }
    }
}
