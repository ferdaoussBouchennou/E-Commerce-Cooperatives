using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Web;

namespace E_Commerce_Cooperatives.Models
{
    public static class EmailHelper
    {
        /// <summary>
        /// Envoie un email de réinitialisation de mot de passe
        /// </summary>
        /// <param name="email">Email du destinataire</param>
        /// <param name="resetToken">Token de réinitialisation</param>
        /// <param name="userName">Nom de l'utilisateur</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        public static bool SendPasswordResetEmail(string email, string resetToken, string userName = "")
        {
            try
            {
                // Construire l'URL de réinitialisation (utilisée dans les deux cas)
                // Encoder le token et l'email pour l'URL
                string baseUrl = HttpContext.Current.Request.Url.Scheme + "://" + 
                                HttpContext.Current.Request.Url.Authority;
                string encodedToken = System.Web.HttpUtility.UrlEncode(resetToken);
                string encodedEmail = System.Web.HttpUtility.UrlEncode(email);
                string resetUrl = baseUrl + "/Account/ResetPassword?token=" + encodedToken + "&email=" + encodedEmail;

                // Récupérer les paramètres SMTP depuis Web.config ou utiliser des valeurs par défaut
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");

                // Si les paramètres SMTP ne sont pas configurés, on simule l'envoi (pour le développement)
                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    // En mode développement, on affiche le lien dans la console/debug
                    System.Diagnostics.Debug.WriteLine("=== EMAIL DE RÉINITIALISATION DE MOT DE PASSE ===");
                    System.Diagnostics.Debug.WriteLine("À: " + email);
                    System.Diagnostics.Debug.WriteLine("Lien de réinitialisation: " + resetUrl);
                    System.Diagnostics.Debug.WriteLine("=============================================");
                    
                    return true; // Simuler le succès en développement
                }

                // Créer le message email
                MailMessage message = new MailMessage();
                message.From = new MailAddress(smtpUsername, "CoopShop");
                message.To.Add(new MailAddress(email));
                message.Subject = "Réinitialisation de votre mot de passe - CoopShop";
                message.IsBodyHtml = true;
                message.Body = GetPasswordResetEmailBody(userName, resetUrl);

                // Configurer le client SMTP
                SmtpClient client = new SmtpClient(smtpServer, smtpPort);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                // Envoyer l'email
                client.Send(message);
                
                // Libérer les ressources
                message.Dispose();
                client.Dispose();
                
                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine("Erreur SMTP lors de l'envoi de l'email: " + smtpEx.Message);
                System.Diagnostics.Debug.WriteLine("Code d'erreur: " + smtpEx.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur lors de l'envoi de l'email: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return false;
            }
        }

        private static string GetPasswordResetEmailBody(string userName, string resetUrl)
        {
            int currentYear = DateTime.Now.Year;
            string greeting = string.IsNullOrEmpty(userName) ? "" : " " + userName;
            
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #305C7D; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #C06C50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>CoopShop</h1>
        </div>
        <div class='content'>
            <h2>Réinitialisation de votre mot de passe</h2>
            <p>Bonjour{greeting},</p>
            <p>Vous avez demandé à réinitialiser votre mot de passe. Cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe :</p>
            <p style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Réinitialiser mon mot de passe</a>
            </p>
            <p>Ou copiez et collez ce lien dans votre navigateur :</p>
            <p style='word-break: break-all; color: #305C7D;'>{resetUrl}</p>
            <p><strong>Ce lien est valide pendant 24 heures.</strong></p>
            <p>Si vous n'avez pas demandé cette réinitialisation, ignorez simplement cet email.</p>
        </div>
        <div class='footer'>
            <p>© {currentYear} CoopShop. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Envoie un email de vérification avec un code à 6 chiffres
        /// </summary>
        /// <param name="email">Email du destinataire</param>
        /// <param name="verificationCode">Code de vérification à 6 chiffres</param>
        /// <param name="userName">Nom de l'utilisateur</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        public static bool SendVerificationEmail(string email, string verificationCode, string userName = "")
        {
            try
            {
                // Récupérer les paramètres SMTP depuis Web.config ou utiliser des valeurs par défaut
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");

                // Si les paramètres SMTP ne sont pas configurés, on simule l'envoi (pour le développement)
                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    // En mode développement, on affiche le code dans la console/debug
                    System.Diagnostics.Debug.WriteLine("=== EMAIL DE VÉRIFICATION ===");
                    System.Diagnostics.Debug.WriteLine("À: " + email);
                    System.Diagnostics.Debug.WriteLine("Code de vérification: " + verificationCode);
                    System.Diagnostics.Debug.WriteLine("=============================================");
                    
                    return true; // Simuler le succès en développement
                }

                // Créer le message email
                MailMessage message = new MailMessage();
                message.From = new MailAddress(smtpUsername, "CoopShop");
                message.To.Add(new MailAddress(email));
                message.Subject = "Vérification de votre email - CoopShop";
                message.IsBodyHtml = true;
                message.Body = GetVerificationEmailBody(userName, verificationCode);

                // Configurer le client SMTP
                SmtpClient client = new SmtpClient(smtpServer, smtpPort);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                // Envoyer l'email
                client.Send(message);
                
                // Libérer les ressources
                message.Dispose();
                client.Dispose();
                
                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine("Erreur SMTP lors de l'envoi de l'email de vérification: " + smtpEx.Message);
                System.Diagnostics.Debug.WriteLine("Code d'erreur: " + smtpEx.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur lors de l'envoi de l'email de vérification: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return false;
            }
        }

        private static string GetVerificationEmailBody(string userName, string verificationCode)
        {
            int currentYear = DateTime.Now.Year;
            string greeting = string.IsNullOrEmpty(userName) ? "" : " " + userName;
            
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #305C7D; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .code-box {{ background-color: white; border: 2px solid #305C7D; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0; }}
        .verification-code {{ font-size: 32px; font-weight: bold; color: #305C7D; letter-spacing: 8px; font-family: 'Courier New', monospace; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>CoopShop</h1>
        </div>
        <div class='content'>
            <h2>Vérification de votre adresse email</h2>
            <p>Bonjour{greeting},</p>
            <p>Merci de vous être inscrit sur CoopShop ! Pour finaliser votre inscription, veuillez entrer le code de vérification ci-dessous :</p>
            <div class='code-box'>
                <p style='margin: 0; color: #666; font-size: 14px;'>Votre code de vérification</p>
                <div class='verification-code'>{verificationCode}</div>
            </div>
            <p>Ce code est valide pendant 15 minutes.</p>
            <p>Si vous n'avez pas créé de compte sur CoopShop, ignorez simplement cet email.</p>
        </div>
        <div class='footer'>
            <p>© {currentYear} CoopShop. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

