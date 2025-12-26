using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Web;
using System.Text;
using System.Collections.Generic;

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
        public static bool SendOrderConfirmationEmail(string toEmail, string clientName, string orderNumber, List<CartItemForOrder> items, decimal subtotal, string modeLivraison, decimal fraisLivraison, decimal totalTTC)
        {
            try
            {
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUsername, "CoopShop");
                mail.To.Add(new MailAddress(toEmail));
                mail.Subject = $"Confirmation de votre commande #{orderNumber} - CoopShop";
                mail.IsBodyHtml = true;

                mail.Body = GenerateOrderEmailBody(clientName, orderNumber, items, subtotal, modeLivraison, fraisLivraison, totalTTC);

                var smtpClient = new SmtpClient(smtpServer, smtpPort);
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = enableSsl;
                smtpClient.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email sending failed: " + ex.Message);
                return false;
            }
        }

        public static bool SendOrderConfirmationEmail(Commande commande)
        {
            try
            {
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUsername, "CoopShop");
                mail.To.Add(new MailAddress(commande.Client.Email));
                mail.Subject = $"Confirmation de votre commande #{commande.NumeroCommande} - CoopShop";
                mail.IsBodyHtml = true;

                // Map items
                // IMPORTANT: Les prix stockés sont HT, on convertit en TTC pour l'affichage dans l'email
                var cartItems = new List<CartItemForOrder>();
                foreach (var item in commande.Items)
                {
                    cartItems.Add(new CartItemForOrder
                    {
                        Nom = item.Produit != null ? item.Produit.Nom : "Produit",
                        Quantite = item.Quantite,
                        PrixUnitaire = item.PrixUnitaireTTC // Utiliser le prix TTC pour l'affichage
                    });
                }

                string clientName = $"{commande.Client.Prenom} {commande.Client.Nom}";
                string modeLivraison = commande.ModeLivraison != null ? commande.ModeLivraison.Nom : "Livraison";

                // Sous-total = TotalHT + MontantTVA (prix TTC des produits, sans frais de livraison)
                decimal sousTotalTTC = Math.Round(commande.TotalHT + commande.MontantTVA, 2);
                mail.Body = GenerateOrderEmailBody(clientName, commande.NumeroCommande, cartItems, sousTotalTTC, modeLivraison, commande.FraisLivraison, commande.TotalTTC);

                // Attach Invoice
                try
                {
                    byte[] invoiceBytes = Helpers.InvoiceHelper.GenerateInvoice(commande);
                    mail.Attachments.Add(new Attachment(new System.IO.MemoryStream(invoiceBytes), $"Facture_{commande.NumeroCommande}.pdf", "application/pdf"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to attach invoice: " + ex.Message);
                }

                var smtpClient = new SmtpClient(smtpServer, smtpPort);
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = enableSsl;
                smtpClient.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email sending failed: " + ex.Message);
                return false;
            }
        }

        private static string GenerateOrderEmailBody(string clientName, string orderNumber, List<CartItemForOrder> items, decimal subtotal, string modeLivraison, decimal fraisLivraison, decimal totalTTC)
        {
            StringBuilder body = new StringBuilder();
            body.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>");
            
            // Header
            body.Append("<div style='background-color: #305C7D; color: white; padding: 20px; text-align: center;'>");
            body.Append("<h1 style='margin: 0;'>Merci pour votre commande !</h1>");
            body.Append("</div>");
            
            // Content
            body.Append("<div style='padding: 20px;'>");
            body.Append($"<p>Bonjour {clientName},</p>");
            body.Append($"<p>Nous avons bien reçu votre commande <strong>#{orderNumber}</strong> et nous la préparons avec soin.</p>");
            
            body.Append("<h3 style='color: #305C7D; border-bottom: 2px solid #C06C50; padding-bottom: 5px;'>Récapitulatif de la commande</h3>");
            body.Append("<table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>");
            body.Append("<thead><tr style='background-color: #f8f9fa;'>");
            body.Append("<th style='text-align: left; padding: 10px; border-bottom: 1px solid #dee2e6;'>Produit</th>");
            body.Append("<th style='text-align: center; padding: 10px; border-bottom: 1px solid #dee2e6;'>Quantité</th>");
            body.Append("<th style='text-align: right; padding: 10px; border-bottom: 1px solid #dee2e6;'>Prix</th>");
            body.Append("</tr></thead><tbody>");

            foreach (var item in items)
            {
                decimal lineTotal = item.PrixUnitaire * item.Quantite;
                body.Append("<tr>");
                body.Append($"<td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.Nom}</td>");
                body.Append($"<td style='padding: 10px; text-align: center; border-bottom: 1px solid #eee;'>{item.Quantite}</td>");
                body.Append($"<td style='padding: 10px; text-align: right; border-bottom: 1px solid #eee;'>{lineTotal:N2} MAD</td>");
                body.Append("</tr>");
            }

            body.Append("</tbody></table>");
            
            // Totals Section
            body.Append("<div style='margin-top: 20px; border-top: 2px solid #eee; padding-top: 10px;'>");
            body.Append("<table style='width: 100%; border-collapse: collapse;'>");
            
            // Sous-total
            body.Append("<tr>");
            body.Append("<td style='padding: 5px 0; color: #666;'>Sous-total :</td>");
            body.Append($"<td style='padding: 5px 0; text-align: right; font-weight: bold;'>{subtotal:N2} MAD</td>");
            body.Append("</tr>");
            
            // Mode de livraison
            body.Append("<tr>");
            body.Append($"<td style='padding: 5px 0; color: #666;'>Livraison ({modeLivraison}) :</td>");
            body.Append($"<td style='padding: 5px 0; text-align: right; font-weight: bold;'>{fraisLivraison:N2} MAD</td>");
            body.Append("</tr>");
            
            // Total Final
            body.Append("<tr style='border-top: 1px solid #eee;'>");
            body.Append("<td style='padding: 15px 0 5px 0; font-size: 1.2rem; color: #305C7D; font-weight: bold;'>Total TTC :</td>");
            body.Append($"<td style='padding: 15px 0 5px 0; text-align: right; font-size: 1.2rem; color: #305C7D; font-weight: bold;'>{totalTTC:N2} MAD</td>");
            body.Append("</tr>");
            
            body.Append("</table>");
            body.Append("</div>");
            
            body.Append("<div style='margin-top: 30px; padding: 20px; background-color: #fcf8e3; border-radius: 4px; color: #8a6d3b;'>");
            body.Append("<p style='margin: 0;'><strong>Prochaines étapes :</strong></p>");
            body.Append("<ul style='margin-top: 10px;'>");
            body.Append("<li>Vous pouvez suivre votre commande sur notre site.</li>");
            body.Append("<li>Vous trouverez votre facture en pièce jointe.</li>");
            body.Append("<li>Notre équipe vous contactera si nécessaire pour la livraison.</li>");
            body.Append("</ul>");
            body.Append("</div>");
            
            body.Append("<p style='margin-top: 30px;'>Si vous avez des questions, n'hésitez pas à répondre à cet email ou à nous contacter via notre service client.</p>");
            body.Append("<p>Cordialement,<br>L'équipe CoopShop</p>");
            body.Append("</div>");
            
            // Footer
            body.Append("<div style='background-color: #f8f9fa; color: #999; padding: 10px; text-align: center; font-size: 0.8rem;'>");
            body.Append("<p>&copy; 2025 CoopShop. Tous droits réservés.</p>");
            body.Append("</div>");
            
            body.Append("</div>");
            
            return body.ToString();
        }

        /// <summary>
        /// Envoie un email d'annulation de commande au client
        /// </summary>
        /// <param name="commande">La commande annulée</param>
        /// <param name="raisonAnnulation">La raison de l'annulation fournie par l'admin</param>
        /// <returns>True si l'email a été envoyé avec succès</returns>
        public static bool SendOrderCancellationEmail(Commande commande, string raisonAnnulation)
        {
            try
            {
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");

                // Si les paramètres SMTP ne sont pas configurés, on simule l'envoi (pour le développement)
                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    System.Diagnostics.Debug.WriteLine("=== EMAIL D'ANNULATION DE COMMANDE ===");
                    System.Diagnostics.Debug.WriteLine("À: " + commande.Client.Email);
                    System.Diagnostics.Debug.WriteLine("Commande: " + commande.NumeroCommande);
                    System.Diagnostics.Debug.WriteLine("Raison: " + raisonAnnulation);
                    System.Diagnostics.Debug.WriteLine("=====================================");
                    return true; // Simuler le succès en développement
                }

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUsername, "CoopShop");
                mail.To.Add(new MailAddress(commande.Client.Email));
                mail.Subject = $"Annulation de votre commande #{commande.NumeroCommande} - CoopShop";
                mail.IsBodyHtml = true;

                string clientName = $"{commande.Client.Prenom} {commande.Client.Nom}";
                mail.Body = GenerateCancellationEmailBody(clientName, commande.NumeroCommande, commande, raisonAnnulation);

                var smtpClient = new SmtpClient(smtpServer, smtpPort);
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = enableSsl;
                smtpClient.Send(mail);

                mail.Dispose();
                smtpClient.Dispose();

                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine("Erreur SMTP lors de l'envoi de l'email d'annulation: " + smtpEx.Message);
                System.Diagnostics.Debug.WriteLine("Code d'erreur: " + smtpEx.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur lors de l'envoi de l'email d'annulation: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return false;
            }
        }

        private static string GenerateCancellationEmailBody(string clientName, string orderNumber, Commande commande, string raisonAnnulation)
        {
            int currentYear = DateTime.Now.Year;
            StringBuilder body = new StringBuilder();
            
            body.Append("<!DOCTYPE html>");
            body.Append("<html>");
            body.Append("<head>");
            body.Append("<meta charset='utf-8'>");
            body.Append("<style>");
            body.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            body.Append(".container { max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; }");
            body.Append(".header { background-color: #dc3545; color: white; padding: 20px; text-align: center; }");
            body.Append(".content { padding: 30px; background-color: #ffffff; }");
            body.Append(".alert-box { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }");
            body.Append(".reason-box { background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; padding: 15px; margin: 20px 0; }");
            body.Append(".reason-title { font-weight: bold; color: #305C7D; margin-bottom: 10px; }");
            body.Append(".reason-text { color: #495057; line-height: 1.8; }");
            body.Append(".order-details { margin-top: 20px; padding-top: 20px; border-top: 2px solid #eee; }");
            body.Append(".order-item { padding: 10px 0; border-bottom: 1px solid #eee; }");
            body.Append(".footer { background-color: #f8f9fa; color: #999; padding: 20px; text-align: center; font-size: 0.8rem; }");
            body.Append("</style>");
            body.Append("</head>");
            body.Append("<body>");
            
            // Container
            body.Append("<div class='container'>");
            
            // Header
            body.Append("<div class='header'>");
            body.Append("<h1 style='margin: 0; font-size: 24px;'>Commande Annulée</h1>");
            body.Append("</div>");
            
            // Content
            body.Append("<div class='content'>");
            body.Append($"<p>Bonjour {clientName},</p>");
            body.Append($"<p>Nous vous informons que votre commande <strong>#{orderNumber}</strong> a été annulée.</p>");
            
            
            // Reason box
            body.Append("<div class='reason-box'>");
            body.Append("<div class='reason-title'>Raison de l'annulation :</div>");
            body.Append($"<div class='reason-text'>{System.Web.HttpUtility.HtmlEncode(raisonAnnulation)}</div>");
            body.Append("</div>");
            
            // Order details
            body.Append("<div class='order-details'>");
            body.Append("<h3 style='color: #305C7D; margin-bottom: 15px;'>Détails de la commande annulée</h3>");
            
            if (commande.Items != null && commande.Items.Count > 0)
            {
                body.Append("<table style='width: 100%; border-collapse: collapse;'>");
                body.Append("<thead><tr style='background-color: #f8f9fa;'>");
                body.Append("<th style='text-align: left; padding: 10px; border-bottom: 1px solid #dee2e6;'>Produit</th>");
                body.Append("<th style='text-align: center; padding: 10px; border-bottom: 1px solid #dee2e6;'>Quantité</th>");
                body.Append("<th style='text-align: right; padding: 10px; border-bottom: 1px solid #dee2e6;'>Prix</th>");
                body.Append("</tr></thead><tbody>");
                
                foreach (var item in commande.Items)
                {
                    decimal lineTotal = item.PrixUnitaireTTC * item.Quantite;
                    body.Append("<tr>");
                    body.Append($"<td style='padding: 10px; border-bottom: 1px solid #eee;'>{System.Web.HttpUtility.HtmlEncode(item.Produit != null ? item.Produit.Nom : "Produit")}</td>");
                    body.Append($"<td style='padding: 10px; text-align: center; border-bottom: 1px solid #eee;'>{item.Quantite}</td>");
                    body.Append($"<td style='padding: 10px; text-align: right; border-bottom: 1px solid #eee;'>{lineTotal:N2} MAD</td>");
                    body.Append("</tr>");
                }
                
                body.Append("</tbody></table>");
                
                // Total
                body.Append("<div style='margin-top: 15px; padding-top: 15px; border-top: 2px solid #eee;'>");
                body.Append("<table style='width: 100%;'>");
                body.Append("<tr>");
                body.Append("<td style='padding: 5px 0; font-weight: bold; color: #305C7D;'>Total de la commande :</td>");
                body.Append($"<td style='padding: 5px 0; text-align: right; font-weight: bold; color: #305C7D;'>{commande.TotalTTC:N2} MAD</td>");
                body.Append("</tr>");
                body.Append("</table>");
                body.Append("</div>");
            }
            
            body.Append("</div>");
            
            // Contact information
            body.Append("<div style='margin-top: 30px; padding: 20px; background-color: #e7f3ff; border-radius: 4px;'>");
            body.Append("<p style='margin: 0 0 10px 0;'><strong>Besoin d'aide ?</strong></p>");
            body.Append("<p style='margin: 0;'>Si vous avez des questions concernant cette annulation, n'hésitez pas à nous contacter via notre service client. Nous sommes là pour vous aider.</p>");
            body.Append("</div>");
            
            body.Append("<p style='margin-top: 30px;'>Cordialement,<br><strong>L'équipe CoopShop</strong></p>");
            body.Append("</div>");
            
            // Footer
            body.Append("<div class='footer'>");
            body.Append($"<p>© {currentYear} CoopShop. Tous droits réservés.</p>");
            body.Append("</div>");
            
            body.Append("</div>");
            body.Append("</body>");
            body.Append("</html>");
            
            return body.ToString();
        }
    }
}

