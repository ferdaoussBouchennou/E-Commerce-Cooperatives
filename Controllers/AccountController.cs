using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;
using E_Commerce_Cooperatives.Models.ViewModels;

namespace E_Commerce_Cooperatives.Controllers
{
    public class AccountController : Controller
    {
        private string connectionString;

        public AccountController()
        {
            var connection = ConfigurationManager.ConnectionStrings["ECommerceConnection"];
            if (connection != null)
            {
                connectionString = connection.ConnectionString;
            }
            else
            {
                connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ecommerce;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            }
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Vérifier si l'email existe déjà
                    var checkEmailQuery = "SELECT COUNT(*) FROM Utilisateurs WHERE Email = @Email";
                    using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", model.Email);
                        var emailExists = (int)checkCommand.ExecuteScalar() > 0;

                        if (emailExists)
                        {
                            ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                            return View(model);
                        }
                    }

                    // Hasher le mot de passe
                    string hashedPassword = PasswordHelper.HashPassword(model.MotDePasse);

                    // Insérer l'utilisateur
                    var insertUserQuery = @"INSERT INTO Utilisateurs (Email, MotDePasse, TypeUtilisateur, DateCreation) 
                                           VALUES (@Email, @MotDePasse, 'Client', GETDATE());
                                           SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    int utilisateurId;
                    using (var userCommand = new SqlCommand(insertUserQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@Email", model.Email);
                        userCommand.Parameters.AddWithValue("@MotDePasse", hashedPassword);
                        utilisateurId = (int)userCommand.ExecuteScalar();
                    }

                    // Insérer le client (non activé jusqu'à vérification email)
                    var insertClientQuery = @"INSERT INTO Clients (UtilisateurId, Nom, Prenom, Telephone, EstActif, DateCreation) 
                                             VALUES (@UtilisateurId, @Nom, @Prenom, @Telephone, 0, GETDATE());
                                             SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    int clientId;
                    using (var clientCommand = new SqlCommand(insertClientQuery, connection))
                    {
                        clientCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        clientCommand.Parameters.AddWithValue("@Nom", model.Nom);
                        clientCommand.Parameters.AddWithValue("@Prenom", model.Prenom);
                        clientCommand.Parameters.AddWithValue("@Telephone", string.IsNullOrEmpty(model.Telephone) ? (object)DBNull.Value : model.Telephone);
                        clientId = (int)clientCommand.ExecuteScalar();
                    }

                    // Générer le code de vérification
                    string verificationCode = PasswordHelper.GenerateVerificationCode();
                    DateTime codeExpiration = DateTime.Now.AddMinutes(15); // Code valide 15 minutes

                    // Stocker le code dans la base de données (utiliser TokenResetPassword temporairement)
                    var updateCodeQuery = @"UPDATE Clients 
                                           SET TokenResetPassword = @VerificationCode, 
                                               DateExpirationToken = @ExpirationDate
                                           WHERE ClientId = @ClientId";
                    
                    using (var codeCommand = new SqlCommand(updateCodeQuery, connection))
                    {
                        codeCommand.Parameters.AddWithValue("@VerificationCode", verificationCode);
                        codeCommand.Parameters.AddWithValue("@ExpirationDate", codeExpiration);
                        codeCommand.Parameters.AddWithValue("@ClientId", clientId);
                        codeCommand.ExecuteNonQuery();
                    }

                    // Envoyer l'email de vérification
                    string userName = model.Prenom + " " + model.Nom;
                    bool emailSent = EmailHelper.SendVerificationEmail(model.Email, verificationCode, userName);

                    // Stocker les informations dans la session pour la vérification
                    Session["PendingUserId"] = utilisateurId;
                    Session["PendingClientId"] = clientId;
                    Session["PendingEmail"] = model.Email;
                    Session["PendingNom"] = model.Nom;
                    Session["PendingPrenom"] = model.Prenom;
                    Session["PendingTelephone"] = model.Telephone;
                    Session["VerificationCode"] = verificationCode;
                    Session["CodeExpiration"] = codeExpiration;

                    if (emailSent)
                    {
                        TempData["InfoMessage"] = "Un code de vérification a été envoyé à votre adresse email. Veuillez l'entrer pour finaliser votre inscription.";
                    }
                    else
                    {
                        // En mode développement, afficher le code
                        TempData["InfoMessage"] = "Mode développement - Code de vérification: " + verificationCode;
                    }

                    return RedirectToAction("VerifyEmail", "Account", new { email = model.Email });
                }
            }
            catch (SqlException sqlEx)
            {
                ModelState.AddModelError("", "Une erreur de base de données est survenue. Veuillez réessayer.");
                System.Diagnostics.Debug.WriteLine("Erreur SQL inscription: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de l'inscription. Veuillez réessayer.");
                System.Diagnostics.Debug.WriteLine("Erreur inscription: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
            }

            return View(model);
        }

        // GET: Account/VerifyEmail
        public ActionResult VerifyEmail(string email)
        {
            // Vérifier si l'utilisateur a une session en attente de vérification
            if (Session["PendingUserId"] == null || Session["PendingEmail"] == null)
            {
                TempData["ErrorMessage"] = "Aucune demande de vérification en cours. Veuillez vous inscrire d'abord.";
                return RedirectToAction("Register", "Account");
            }

            var model = new VerifyEmailViewModel
            {
                Email = Session["PendingEmail"]?.ToString() ?? email
            };

            return View(model);
        }

        // POST: Account/VerifyEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Vérifier si la session existe toujours
                if (Session["PendingUserId"] == null || Session["PendingEmail"] == null || Session["VerificationCode"] == null)
                {
                    ModelState.AddModelError("", "Votre session a expiré. Veuillez vous réinscrire.");
                    return RedirectToAction("Register", "Account");
                }

                int pendingUserId = (int)Session["PendingUserId"];
                int pendingClientId = (int)Session["PendingClientId"];
                string pendingEmail = Session["PendingEmail"].ToString();
                string storedCode = Session["VerificationCode"].ToString();
                DateTime codeExpiration = (DateTime)Session["CodeExpiration"];

                // Vérifier l'expiration du code
                if (DateTime.Now > codeExpiration)
                {
                    ModelState.AddModelError("", "Le code de vérification a expiré. Veuillez demander un nouveau code.");
                    // Nettoyer la session
                    Session.Remove("PendingUserId");
                    Session.Remove("PendingClientId");
                    Session.Remove("PendingEmail");
                    Session.Remove("PendingNom");
                    Session.Remove("PendingPrenom");
                    Session.Remove("VerificationCode");
                    Session.Remove("CodeExpiration");
                    return View(model);
                }

                // Vérifier le code
                if (model.VerificationCode.Trim() != storedCode.Trim())
                {
                    ModelState.AddModelError("VerificationCode", "Le code de vérification est incorrect. Veuillez réessayer.");
                    return View(model);
                }

                // Vérifier également dans la base de données
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var verifyCodeQuery = @"SELECT c.ClientId, c.TokenResetPassword, c.DateExpirationToken
                                          FROM Clients c
                                          INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                                          WHERE u.UtilisateurId = @UtilisateurId AND u.Email = @Email";
                    
                    using (var command = new SqlCommand(verifyCodeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UtilisateurId", pendingUserId);
                        command.Parameters.AddWithValue("@Email", pendingEmail);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(1))
                                {
                                    string dbCode = reader.GetString(1);
                                    
                                    if (dbCode.Trim() != model.VerificationCode.Trim())
                                    {
                                        ModelState.AddModelError("VerificationCode", "Le code de vérification est incorrect.");
                                        return View(model);
                                    }

                                    // Vérifier l'expiration dans la DB
                                    if (!reader.IsDBNull(2))
                                    {
                                        DateTime dbExpiration = reader.GetDateTime(2);
                                        if (DateTime.Now > dbExpiration)
                                        {
                                            ModelState.AddModelError("", "Le code de vérification a expiré. Veuillez demander un nouveau code.");
                                            return View(model);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Activer le compte et nettoyer le code
                    var activateAccountQuery = @"UPDATE Clients 
                                               SET EstActif = 1, 
                                                   TokenResetPassword = NULL, 
                                                   DateExpirationToken = NULL
                                               WHERE ClientId = @ClientId";
                    
                    using (var activateCommand = new SqlCommand(activateAccountQuery, connection))
                    {
                        activateCommand.Parameters.AddWithValue("@ClientId", pendingClientId);
                        activateCommand.ExecuteNonQuery();
                    }
                }

                // Créer la session de connexion
                Session["UserId"] = pendingUserId;
                Session["ClientId"] = pendingClientId;
                Session["Email"] = pendingEmail;
                Session["Nom"] = Session["PendingNom"];
                Session["Prenom"] = Session["PendingPrenom"];
                Session["ClientNom"] = Session["PendingPrenom"] + " " + Session["PendingNom"];
                Session["TypeUtilisateur"] = "Client";
                // Ajouter le téléphone à la session
                if (Session["PendingTelephone"] != null)
                {
                    Session["Telephone"] = Session["PendingTelephone"];
                }

                // Nettoyer la session de vérification
                Session.Remove("PendingUserId");
                Session.Remove("PendingClientId");
                Session.Remove("PendingEmail");
                Session.Remove("PendingNom");
                Session.Remove("PendingPrenom");
                Session.Remove("PendingTelephone");
                Session.Remove("VerificationCode");
                Session.Remove("CodeExpiration");

                TempData["SuccessMessage"] = "Votre email a été vérifié avec succès ! Votre compte est maintenant actif.";
                return RedirectToAction("Index", "Home");
            }
            catch (SqlException sqlEx)
            {
                ModelState.AddModelError("", "Une erreur de base de données est survenue. Veuillez réessayer.");
                System.Diagnostics.Debug.WriteLine("Erreur SQL vérification email: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de la vérification. Veuillez réessayer.");
                System.Diagnostics.Debug.WriteLine("Erreur vérification email: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
            }

            return View(model);
        }

        // GET: Account/ResendVerificationCode
        public ActionResult ResendVerificationCode(string email)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Récupérer les informations du client
                    var getClientQuery = @"SELECT c.ClientId, c.UtilisateurId, c.Nom, c.Prenom, u.Email, c.Telephone
                                         FROM Clients c
                                         INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                                         WHERE u.Email = @Email AND c.EstActif = 0";
                    
                    using (var command = new SqlCommand(getClientQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int clientId = reader.GetInt32(0);
                                int utilisateurId = reader.GetInt32(1);
                                string nom = reader.GetString(2);
                                string prenom = reader.GetString(3);
                                string clientEmail = reader.GetString(4);
                                string telephone = !reader.IsDBNull(5) ? reader.GetString(5) : null;

                                reader.Close();

                                // Générer un nouveau code
                                string verificationCode = PasswordHelper.GenerateVerificationCode();
                                DateTime codeExpiration = DateTime.Now.AddMinutes(15);

                                // Mettre à jour le code dans la base de données
                                var updateCodeQuery = @"UPDATE Clients 
                                                       SET TokenResetPassword = @VerificationCode, 
                                                           DateExpirationToken = @ExpirationDate
                                                       WHERE ClientId = @ClientId";
                                
                                using (var updateCommand = new SqlCommand(updateCodeQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@VerificationCode", verificationCode);
                                    updateCommand.Parameters.AddWithValue("@ExpirationDate", codeExpiration);
                                    updateCommand.Parameters.AddWithValue("@ClientId", clientId);
                                    updateCommand.ExecuteNonQuery();
                                }

                                // Envoyer l'email
                                string userName = prenom + " " + nom;
                                bool emailSent = EmailHelper.SendVerificationEmail(clientEmail, verificationCode, userName);

                                // Mettre à jour la session
                                Session["PendingUserId"] = utilisateurId;
                                Session["PendingClientId"] = clientId;
                                Session["PendingEmail"] = clientEmail;
                                Session["PendingNom"] = nom;
                                Session["PendingPrenom"] = prenom;
                                Session["PendingTelephone"] = telephone;
                                Session["VerificationCode"] = verificationCode;
                                Session["CodeExpiration"] = codeExpiration;

                                if (emailSent)
                                {
                                    TempData["InfoMessage"] = "Un nouveau code de vérification a été envoyé à votre adresse email.";
                                }
                                else
                                {
                                    TempData["InfoMessage"] = "Mode développement - Nouveau code: " + verificationCode;
                                }

                                return RedirectToAction("VerifyEmail", "Account", new { email = clientEmail });
                            }
                        }
                    }
                }

                TempData["ErrorMessage"] = "Aucun compte en attente de vérification trouvé pour cet email.";
                return RedirectToAction("Register", "Account");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur ResendVerificationCode: " + ex.Message);
                TempData["ErrorMessage"] = "Une erreur est survenue. Veuillez réessayer.";
                return RedirectToAction("VerifyEmail", "Account", new { email = email });
            }
        }

        // GET: Account/Login
        public ActionResult Login(string returnUrl = null, string message = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (message == "PasswordResetSuccess")
            {
                ViewBag.SuccessMessage = "Votre mot de passe a été réinitialisé avec succès. Vous pouvez maintenant vous connecter.";
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Récupérer l'utilisateur
                    var loginQuery = @"SELECT u.UtilisateurId, u.MotDePasse, u.TypeUtilisateur, 
                                      c.ClientId, c.Nom, c.Prenom, c.EstActif, c.TokenResetPassword, c.Telephone
                                      FROM Utilisateurs u
                                      LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                                      WHERE u.Email = @Email";

                    using (var command = new SqlCommand(loginQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Email", model.Email);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int utilisateurId = reader.GetInt32(0);
                                string storedPassword = reader.GetString(1);
                                string typeUtilisateur = reader.GetString(2);
                                
                                // Vérifier si le compte est actif (pour les clients uniquement)
                                if (typeUtilisateur == "Client")
                                {
                                    if (!reader.IsDBNull(6))
                                    {
                                        bool estActif = reader.GetBoolean(6);
                                        if (!estActif)
                                        {
                                            // Vérifier si un code de vérification existe
                                            bool hasVerificationCode = !reader.IsDBNull(7) && !string.IsNullOrEmpty(reader.GetString(7));
                                            
                                            if (hasVerificationCode)
                                            {
                                                ModelState.AddModelError("", "Votre email n'a pas encore été vérifié. Veuillez vérifier votre email pour activer votre compte.");
                                                // Stocker les infos pour la vérification
                                                Session["PendingUserId"] = utilisateurId;
                                                Session["PendingEmail"] = model.Email;
                                                TempData["ErrorMessage"] = "Votre email n'a pas encore été vérifié. Veuillez entrer le code de vérification.";
                                                return RedirectToAction("VerifyEmail", "Account", new { email = model.Email });
                                            }
                                            else
                                            {
                                                ModelState.AddModelError("", "Votre compte a été désactivé. Veuillez contacter l'administrateur.");
                                                return View(model);
                                            }
                                        }
                                    }
                                }

                                // Vérifier le mot de passe (support plain text pour compatibilité avec données existantes)
                                bool passwordValid = false;
                                
                                if (storedPassword.Length == 64) // SHA256 hash is 64 characters (hex)
                                {
                                    // Password is hashed
                                    passwordValid = PasswordHelper.VerifyPassword(model.MotDePasse, storedPassword);
                                }
                                else
                                {
                                    // Password is plain text (for backward compatibility with existing test data)
                                    passwordValid = model.MotDePasse == storedPassword;
                                    
                                    // If plain text password is valid, update it to hashed version
                                    if (passwordValid)
                                    {
                                        reader.Close();
                                        string hashedPassword = PasswordHelper.HashPassword(model.MotDePasse);
                                        var updatePasswordQuery = "UPDATE Utilisateurs SET MotDePasse = @HashedPassword WHERE UtilisateurId = @UtilisateurId";
                                        using (var updateCommand = new SqlCommand(updatePasswordQuery, connection))
                                        {
                                            updateCommand.Parameters.AddWithValue("@HashedPassword", hashedPassword);
                                            updateCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                                            updateCommand.ExecuteNonQuery();
                                        }
                                        // Re-execute the query to get updated data
                                        using (var command2 = new SqlCommand(loginQuery, connection))
                                        {
                                            command2.Parameters.AddWithValue("@Email", model.Email);
                                            using (var reader2 = command2.ExecuteReader())
                                            {
                                                if (reader2.Read())
                                                {
                                                    utilisateurId = reader2.GetInt32(0);
                                                    typeUtilisateur = reader2.GetString(2);

                                                    // Créer la session
                                                    Session["UserId"] = utilisateurId;
                                                    Session["TypeUtilisateur"] = typeUtilisateur;
                                                    Session["Email"] = model.Email;

                                                    if (!reader2.IsDBNull(3))
                                                    {
                                                        Session["ClientId"] = reader2.GetInt32(3);
                                                        Session["Nom"] = reader2.GetString(4);
                                                        Session["Prenom"] = reader2.GetString(5);
                                                        Session["ClientNom"] = reader2.GetString(5) + " " + reader2.GetString(4);
                                                        // Ajouter le téléphone à la session
                                                        if (!reader2.IsDBNull(8))
                                                        {
                                                            Session["Telephone"] = reader2.GetString(8);
                                                        }
                                                    }

                                                    // Handle "Remember Me"
                                                    if (model.SeSouvenirDeMoi)
                                                    {
                                                        HttpCookie cookie = new HttpCookie("RememberMe");
                                                        cookie["UserId"] = utilisateurId.ToString();
                                                        cookie.Expires = DateTime.Now.AddDays(30);
                                                        Response.Cookies.Add(cookie);
                                                    }

                                                    // Mettre à jour la dernière connexion (seulement pour les clients)
                                                    if (typeUtilisateur == "Client")
                                                    {
                                                        UpdateLastLogin(connection, utilisateurId);
                                                    }

                                                    // Redirection selon le type d'utilisateur
                                                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                                    {
                                                        return Redirect(returnUrl);
                                                    }

                                                    if (typeUtilisateur == "Admin")
                                                    {
                                                        return RedirectToAction("Index", "Admin");
                                                    }
                                                    else
                                                    {
                                                        return RedirectToAction("Index", "Home");
                                                    }
                                                }
                                            }
                                        }
                                        return View(model);
                                    }
                                }
                                
                                if (passwordValid)
                                {
                                    // Créer la session
                                    Session["UserId"] = utilisateurId;
                                    Session["TypeUtilisateur"] = typeUtilisateur;
                                    Session["Email"] = model.Email;

                                    if (!reader.IsDBNull(3))
                                    {
                                        Session["ClientId"] = reader.GetInt32(3);
                                        Session["Nom"] = reader.GetString(4);
                                        Session["Prenom"] = reader.GetString(5);
                                        Session["ClientNom"] = reader.GetString(5) + " " + reader.GetString(4);
                                        // Ajouter le téléphone à la session
                                        if (!reader.IsDBNull(8))
                                        {
                                            Session["Telephone"] = reader.GetString(8);
                                        }
                                    }

                                    // Handle "Remember Me"
                                    if (model.SeSouvenirDeMoi)
                                    {
                                        HttpCookie cookie = new HttpCookie("RememberMe");
                                        cookie["UserId"] = utilisateurId.ToString();
                                        cookie.Expires = DateTime.Now.AddDays(30);
                                        Response.Cookies.Add(cookie);
                                    }

                                    // Mettre à jour la dernière connexion (seulement pour les clients)
                                    reader.Close();
                                    if (typeUtilisateur == "Client")
                                    {
                                        UpdateLastLogin(connection, utilisateurId);
                                    }

                                    // Redirection selon le type d'utilisateur
                                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                    {
                                        return Redirect(returnUrl);
                                    }

                                    if (typeUtilisateur == "Admin")
                                    {
                                        return RedirectToAction("Index", "Admin");
                                    }
                                    else
                                    {
                                        return RedirectToAction("Index", "Home");
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Email ou mot de passe incorrect.");
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("", "Email ou mot de passe incorrect.");
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                ModelState.AddModelError("", "Une erreur de base de données est survenue. Veuillez réessayer.");
                System.Diagnostics.Debug.WriteLine("Erreur SQL login: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de la connexion. Veuillez réessayer.");
                System.Diagnostics.Debug.WriteLine("Erreur login: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
            }

            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            // Remove remember me cookie
            if (Request.Cookies["RememberMe"] != null)
            {
                HttpCookie cookie = new HttpCookie("RememberMe");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogoutPost()
        {
            Session.Clear();
            Session.Abandon();

            // Remove remember me cookie
            if (Request.Cookies["RememberMe"] != null)
            {
                HttpCookie cookie = new HttpCookie("RememberMe");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Vérifier si l'email existe
                        var checkEmailQuery = @"SELECT c.ClientId, c.Nom, c.Prenom 
                                               FROM Clients c
                                               INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                                               WHERE u.Email = @Email";
                        
                        using (var command = new SqlCommand(checkEmailQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Email", model.Email);
                            
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int clientId = reader.GetInt32(0);
                                    string nom = reader.GetString(1);
                                    string prenom = reader.GetString(2);
                                    
                                    reader.Close();

                                    // Générer un token de réinitialisation
                                    string resetToken = PasswordHelper.GenerateResetToken();
                                    DateTime expirationDate = DateTime.Now.AddHours(24); // Token valide 24h

                                    // Sauvegarder le token dans la base de données
                                    var updateTokenQuery = @"UPDATE Clients 
                                                           SET TokenResetPassword = @Token, 
                                                               DateExpirationToken = @ExpirationDate
                                                           WHERE ClientId = @ClientId";
                                    
                                    using (var updateCommand = new SqlCommand(updateTokenQuery, connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@Token", resetToken);
                                        updateCommand.Parameters.AddWithValue("@ExpirationDate", expirationDate);
                                        updateCommand.Parameters.AddWithValue("@ClientId", clientId);
                                        updateCommand.ExecuteNonQuery();
                                    }

                                    // Envoyer l'email de réinitialisation
                                    string userName = prenom + " " + nom;
                                    bool emailSent = EmailHelper.SendPasswordResetEmail(model.Email, resetToken, userName);

                                    if (emailSent)
                                    {
                                        ViewBag.SuccessMessage = "Un email de réinitialisation a été envoyé à " + model.Email + ". Veuillez vérifier votre boîte de réception.";
                                    }
                                    else
                                    {
                                        // En mode développement, afficher le lien directement
                                        string resetUrl = Request.Url.Scheme + "://" + Request.Url.Authority + 
                                                         "/Account/ResetPassword?token=" + resetToken + "&email=" + model.Email;
                                        ViewBag.SuccessMessage = "Mode développement : Lien de réinitialisation : " + resetUrl;
                                    }
                                }
                                else
                                {
                                    // Ne pas révéler si l'email existe ou non (sécurité)
                                    ViewBag.SuccessMessage = "Si cet email existe dans notre système, un lien de réinitialisation a été envoyé.";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Une erreur est survenue. Veuillez réessayer plus tard.");
                    System.Diagnostics.Debug.WriteLine("Erreur ForgotPassword: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Account/ResetPassword
        public ActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Lien de réinitialisation invalide.";
                return View("Error");
            }

            // Décoder le token et l'email depuis l'URL (ils peuvent être encodés)
            string decodedToken = token;
            string decodedEmail = email;
            try
            {
                decodedToken = System.Web.HttpUtility.UrlDecode(token);
                decodedEmail = System.Web.HttpUtility.UrlDecode(email);
            }
            catch
            {
                // Si le décodage échoue, utiliser les valeurs originales
            }

            // Vérifier si le token est valide
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Chercher le token en comparant avec les versions encodées et décodées
                    var checkTokenQuery = @"SELECT c.ClientId, c.DateExpirationToken, u.Email, c.TokenResetPassword
                                          FROM Clients c
                                          INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                                          WHERE u.Email = @Email AND c.TokenResetPassword IS NOT NULL";
                    
                    using (var command = new SqlCommand(checkTokenQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Email", decodedEmail);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(3))
                                {
                                    string storedToken = reader.GetString(3);
                                    
                                    // Comparer les tokens (gérer les cas où le token peut être encodé)
                                    // Le token peut venir de l'URL encodé ou décodé
                                    // Base64 peut contenir +, /, = qui sont encodés en URL
                                    string storedTokenTrimmed = storedToken?.Trim();
                                    string decodedTokenTrimmed = decodedToken?.Trim();
                                    string tokenTrimmed = token?.Trim();
                                    
                                    bool tokenMatches = (storedTokenTrimmed == decodedTokenTrimmed) || 
                                                       (storedTokenTrimmed == tokenTrimmed) ||
                                                       (System.Web.HttpUtility.UrlEncode(storedTokenTrimmed) == tokenTrimmed) ||
                                                       (System.Web.HttpUtility.UrlDecode(storedTokenTrimmed) == decodedTokenTrimmed) ||
                                                       (storedTokenTrimmed.Replace("+", "%2B").Replace("/", "%2F").Replace("=", "%3D") == tokenTrimmed);
                                    
                                    // Debug logging
                                    if (!tokenMatches)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Token mismatch - Stored: " + storedTokenTrimmed + ", Received: " + tokenTrimmed + ", Decoded: " + decodedTokenTrimmed);
                                    }
                                    
                                    if (tokenMatches)
                                    {
                                        // Vérifier la date d'expiration
                                        if (reader.IsDBNull(1))
                                        {
                                            ViewBag.ErrorMessage = "Lien de réinitialisation invalide.";
                                            return View("Error");
                                        }
                                        
                                        DateTime expirationDate = reader.GetDateTime(1);
                                        
                                        if (expirationDate < DateTime.Now)
                                        {
                                            ViewBag.ErrorMessage = "Ce lien de réinitialisation a expiré. Veuillez demander un nouveau lien.";
                                            return View("Error");
                                        }

                                        // Token valide, afficher le formulaire
                                        var model = new ResetPasswordViewModel
                                        {
                                            Token = storedToken, // Utiliser le token stocké (non encodé)
                                            Email = decodedEmail
                                        };
                                        return View(model);
                                    }
                                }
                            }
                            
                            // Aucun token correspondant trouvé
                            System.Diagnostics.Debug.WriteLine("Token non trouvé. Token reçu: " + token + ", Email: " + decodedEmail);
                            ViewBag.ErrorMessage = "Lien de réinitialisation invalide ou expiré.";
                            return View("Error");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Une erreur est survenue. Veuillez réessayer.";
                System.Diagnostics.Debug.WriteLine("Erreur ResetPassword GET: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return View("Error");
            }
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Vérifier à nouveau le token
                        var checkTokenQuery = @"SELECT c.ClientId, c.DateExpirationToken, u.UtilisateurId, u.Email, c.TokenResetPassword
                                              FROM Clients c
                                              INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                                              WHERE u.Email = @Email AND c.TokenResetPassword IS NOT NULL";
                        
                        using (var command = new SqlCommand(checkTokenQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Email", model.Email);
                            
                            using (var reader = command.ExecuteReader())
                            {
                                bool tokenFound = false;
                                int clientId = 0;
                                int utilisateurId = 0;
                                
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(4))
                                    {
                                        string storedToken = reader.GetString(4);
                                        
                                        // Comparer les tokens (gérer l'encodage URL)
                                        // Base64 peut contenir +, /, = qui sont encodés en URL
                                        string storedTokenTrimmed = storedToken?.Trim();
                                        string modelTokenTrimmed = model.Token?.Trim();
                                        
                                        bool tokenMatches = (storedTokenTrimmed == modelTokenTrimmed) ||
                                                           (System.Web.HttpUtility.UrlDecode(storedTokenTrimmed) == modelTokenTrimmed) ||
                                                           (System.Web.HttpUtility.UrlEncode(storedTokenTrimmed) == modelTokenTrimmed) ||
                                                           (storedTokenTrimmed == System.Web.HttpUtility.UrlDecode(modelTokenTrimmed)) ||
                                                           (storedTokenTrimmed.Replace("+", "%2B").Replace("/", "%2F").Replace("=", "%3D") == modelTokenTrimmed);
                                        
                                        if (tokenMatches)
                                        {
                                            tokenFound = true;
                                            clientId = reader.GetInt32(0);
                                            
                                            // Vérifier la date d'expiration
                                            if (reader.IsDBNull(1))
                                            {
                                                ModelState.AddModelError("", "Lien de réinitialisation invalide.");
                                                return View(model);
                                            }
                                            
                                            DateTime expirationDate = reader.GetDateTime(1);
                                            utilisateurId = reader.GetInt32(2);
                                            
                                            if (expirationDate < DateTime.Now)
                                            {
                                                ModelState.AddModelError("", "Ce lien de réinitialisation a expiré. Veuillez demander un nouveau lien.");
                                                return View(model);
                                            }
                                            break; // Token trouvé, sortir de la boucle
                                        }
                                    }
                                }
                                
                                reader.Close();
                                
                                if (!tokenFound)
                                {
                                    ModelState.AddModelError("", "Lien de réinitialisation invalide ou expiré.");
                                    return View(model);
                                }

                                // Hasher le nouveau mot de passe
                                string hashedPassword = PasswordHelper.HashPassword(model.Password);

                                // Mettre à jour le mot de passe
                                var updatePasswordQuery = @"UPDATE Utilisateurs 
                                                          SET MotDePasse = @MotDePasse 
                                                          WHERE UtilisateurId = @UtilisateurId";
                                
                                using (var updateCommand = new SqlCommand(updatePasswordQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@MotDePasse", hashedPassword);
                                    updateCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                                    updateCommand.ExecuteNonQuery();
                                }

                                // Supprimer le token (utilisé une seule fois)
                                var clearTokenQuery = @"UPDATE Clients 
                                                      SET TokenResetPassword = NULL, 
                                                          DateExpirationToken = NULL
                                                      WHERE ClientId = @ClientId";
                                
                                using (var clearCommand = new SqlCommand(clearTokenQuery, connection))
                                {
                                    clearCommand.Parameters.AddWithValue("@ClientId", clientId);
                                    clearCommand.ExecuteNonQuery();
                                }

                                ViewBag.SuccessMessage = "Votre mot de passe a été réinitialisé avec succès. Vous pouvez maintenant vous connecter.";
                                return RedirectToAction("Login", "Account", new { message = "PasswordResetSuccess" });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Une erreur est survenue lors de la réinitialisation. Veuillez réessayer.");
                    System.Diagnostics.Debug.WriteLine("Erreur ResetPassword POST: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                }
            }

            return View(model);
        }

        private void UpdateLastLogin(SqlConnection connection, int utilisateurId)
        {
            try
            {
                var updateQuery = @"UPDATE Clients 
                                   SET DerniereConnexion = GETDATE() 
                                   WHERE UtilisateurId = @UtilisateurId";
                
                using (var command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignorer les erreurs de mise à jour de la dernière connexion
            }
        }
    }
}

