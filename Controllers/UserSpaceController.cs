using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;
using E_Commerce_Cooperatives.Models.ViewModels;

namespace E_Commerce_Cooperatives.Controllers
{
    public class UserSpaceController : Controller
    {
        private string connectionString;

        public UserSpaceController()
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

        // Vérifier que l'utilisateur est connecté
        private bool IsUserAuthenticated()
        {
            return Session["UserId"] != null && Session["ClientId"] != null;
        }

        private ActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Request.Url?.PathAndQuery });
        }

        // GET: UserSpace/Profile
        public ActionResult Profile()
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            int clientId = (int)Session["ClientId"];
            int utilisateurId = (int)Session["UserId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"SELECT c.ClientId, c.Nom, c.Prenom, c.Telephone, c.DateNaissance, 
                                         c.DateCreation, c.DerniereConnexion, u.Email
                                  FROM Clients c
                                  INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                                  WHERE c.ClientId = @ClientId";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var model = new ProfileViewModel
                                {
                                    ClientId = reader.GetInt32(0),
                                    Nom = reader.GetString(1),
                                    Prenom = reader.GetString(2),
                                    Telephone = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    DateNaissance = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                    DateCreation = reader.GetDateTime(5),
                                    DerniereConnexion = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                    Email = reader.GetString(7)
                                };
                                
                                // Store DateCreation in ViewBag for sidebar
                                ViewBag.DateCreation = model.DateCreation;
                                
                                return View(model);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement du profil.";
                System.Diagnostics.Debug.WriteLine("Erreur Profile GET: " + ex.Message);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: UserSpace/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int clientId = (int)Session["ClientId"];
            int utilisateurId = (int)Session["UserId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Vérifier si l'email existe déjà pour un autre utilisateur
                    var checkEmailQuery = @"SELECT COUNT(*) FROM Utilisateurs 
                                           WHERE Email = @Email AND UtilisateurId != @UtilisateurId";
                    using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", model.Email);
                        checkCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        var emailExists = (int)checkCommand.ExecuteScalar() > 0;

                        if (emailExists)
                        {
                            ModelState.AddModelError("Email", "Cet email est déjà utilisé par un autre compte.");
                            return View(model);
                        }
                    }

                    // Mettre à jour l'email dans Utilisateurs
                    var updateUserQuery = "UPDATE Utilisateurs SET Email = @Email WHERE UtilisateurId = @UtilisateurId";
                    using (var userCommand = new SqlCommand(updateUserQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@Email", model.Email);
                        userCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        userCommand.ExecuteNonQuery();
                    }

                    // Mettre à jour les informations du client
                    var updateClientQuery = @"UPDATE Clients 
                                             SET Nom = @Nom, Prenom = @Prenom, Telephone = @Telephone, 
                                                 DateNaissance = @DateNaissance
                                             WHERE ClientId = @ClientId";
                    using (var clientCommand = new SqlCommand(updateClientQuery, connection))
                    {
                        clientCommand.Parameters.AddWithValue("@Nom", model.Nom);
                        clientCommand.Parameters.AddWithValue("@Prenom", model.Prenom);
                        clientCommand.Parameters.AddWithValue("@Telephone", string.IsNullOrEmpty(model.Telephone) ? (object)DBNull.Value : model.Telephone);
                        clientCommand.Parameters.AddWithValue("@DateNaissance", model.DateNaissance.HasValue ? (object)model.DateNaissance.Value : DBNull.Value);
                        clientCommand.Parameters.AddWithValue("@ClientId", clientId);
                        clientCommand.ExecuteNonQuery();
                    }

                    // Mettre à jour la session
                    Session["Email"] = model.Email;
                    Session["Nom"] = model.Nom;
                    Session["Prenom"] = model.Prenom;
                    Session["ClientNom"] = model.Prenom + " " + model.Nom;

                    TempData["SuccessMessage"] = "Votre profil a été mis à jour avec succès.";
                    return RedirectToAction("Profile");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour du profil.");
                System.Diagnostics.Debug.WriteLine("Erreur Profile POST: " + ex.Message);
            }

            return View(model);
        }

        // GET: UserSpace/Addresses
        public ActionResult Addresses()
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            int clientId = (int)Session["ClientId"];
            var addresses = new List<AddressViewModel>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"SELECT AdresseId, AdresseComplete, Ville, CodePostal, Pays, EstParDefaut, DateCreation
                                  FROM Adresses
                                  WHERE ClientId = @ClientId
                                  ORDER BY EstParDefaut DESC, DateCreation DESC";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                addresses.Add(new AddressViewModel
                                {
                                    AdresseId = reader.GetInt32(0),
                                    AdresseComplete = reader.GetString(1),
                                    Ville = reader.GetString(2),
                                    CodePostal = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Pays = reader.IsDBNull(4) ? "Maroc" : reader.GetString(4),
                                    EstParDefaut = reader.GetBoolean(5),
                                    DateCreation = reader.GetDateTime(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement des adresses.";
                System.Diagnostics.Debug.WriteLine("Erreur Addresses GET: " + ex.Message);
            }

            // Si requête AJAX, retourner seulement le contenu
            if (Request.IsAjaxRequest())
            {
                return PartialView("_AddressesContent", addresses);
            }

            return View(addresses);
        }

        // PARTIAL: Addresses (pour onglet dynamique)
        [HttpGet]
        public ActionResult AddressesPartial()
        {
            if (!IsUserAuthenticated())
                return new HttpStatusCodeResult(401);

            int clientId = (int)Session["ClientId"];
            var addresses = new List<AddressViewModel>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"SELECT AdresseId, AdresseComplete, Ville, CodePostal, Pays, EstParDefaut, DateCreation
                                  FROM Adresses
                                  WHERE ClientId = @ClientId
                                  ORDER BY EstParDefaut DESC, DateCreation DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                addresses.Add(new AddressViewModel
                                {
                                    AdresseId = reader.GetInt32(0),
                                    AdresseComplete = reader.GetString(1),
                                    Ville = reader.GetString(2),
                                    CodePostal = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Pays = reader.IsDBNull(4) ? "Maroc" : reader.GetString(4),
                                    EstParDefaut = reader.GetBoolean(5),
                                    DateCreation = reader.GetDateTime(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur AddressesPartial GET: " + ex.Message);
                return new HttpStatusCodeResult(500, "Erreur lors du chargement des adresses");
            }

            return PartialView("_AddressesContent", addresses);
        }

        // GET: UserSpace/AddAddress
        public ActionResult AddAddress()
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            return View(new AddressViewModel { Pays = "Maroc" });
        }

        // POST: UserSpace/AddAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAddress(AddressViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int clientId = (int)Session["ClientId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Si cette adresse est définie comme par défaut, retirer le statut par défaut des autres adresses
                    if (model.EstParDefaut)
                    {
                        var removeDefaultQuery = "UPDATE Adresses SET EstParDefaut = 0 WHERE ClientId = @ClientId";
                        using (var removeCommand = new SqlCommand(removeDefaultQuery, connection))
                        {
                            removeCommand.Parameters.AddWithValue("@ClientId", clientId);
                            removeCommand.ExecuteNonQuery();
                        }
                    }
                    // Si c'est la première adresse, la définir comme par défaut
                    else
                    {
                        var countQuery = "SELECT COUNT(*) FROM Adresses WHERE ClientId = @ClientId";
                        using (var countCommand = new SqlCommand(countQuery, connection))
                        {
                            countCommand.Parameters.AddWithValue("@ClientId", clientId);
                            var count = (int)countCommand.ExecuteScalar();
                            if (count == 0)
                            {
                                model.EstParDefaut = true;
                            }
                        }
                    }

                    var insertQuery = @"INSERT INTO Adresses (ClientId, AdresseComplete, Ville, CodePostal, Pays, EstParDefaut, DateCreation)
                                       VALUES (@ClientId, @AdresseComplete, @Ville, @CodePostal, @Pays, @EstParDefaut, GETDATE())";
                    using (var command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        command.Parameters.AddWithValue("@AdresseComplete", model.AdresseComplete);
                        command.Parameters.AddWithValue("@Ville", model.Ville);
                        command.Parameters.AddWithValue("@CodePostal", string.IsNullOrEmpty(model.CodePostal) ? (object)DBNull.Value : model.CodePostal);
                        command.Parameters.AddWithValue("@Pays", string.IsNullOrEmpty(model.Pays) ? "Maroc" : model.Pays);
                        command.Parameters.AddWithValue("@EstParDefaut", model.EstParDefaut);
                        command.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] = "L'adresse a été ajoutée avec succès.";
                    // Retour à la page Profil sur l’onglet adresses
                    return RedirectToAction("Profile", new { tab = "adresses" });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de l'ajout de l'adresse.");
                System.Diagnostics.Debug.WriteLine("Erreur AddAddress POST: " + ex.Message);
            }

            return View(model);
        }

        // GET: UserSpace/EditAddress
        public ActionResult EditAddress(int id)
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            int clientId = (int)Session["ClientId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"SELECT AdresseId, AdresseComplete, Ville, CodePostal, Pays, EstParDefaut, DateCreation
                                  FROM Adresses
                                  WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AdresseId", id);
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var model = new AddressViewModel
                                {
                                    AdresseId = reader.GetInt32(0),
                                    AdresseComplete = reader.GetString(1),
                                    Ville = reader.GetString(2),
                                    CodePostal = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Pays = reader.IsDBNull(4) ? "Maroc" : reader.GetString(4),
                                    EstParDefaut = reader.GetBoolean(5),
                                    DateCreation = reader.GetDateTime(6)
                                };
                                return View(model);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement de l'adresse.";
                System.Diagnostics.Debug.WriteLine("Erreur EditAddress GET: " + ex.Message);
            }

            return RedirectToAction("Addresses");
        }

        // POST: UserSpace/EditAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAddress(AddressViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int clientId = (int)Session["ClientId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Vérifier que l'adresse appartient au client
                    var checkQuery = "SELECT COUNT(*) FROM Adresses WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@AdresseId", model.AdresseId);
                        checkCommand.Parameters.AddWithValue("@ClientId", clientId);
                        var exists = (int)checkCommand.ExecuteScalar() > 0;

                        if (!exists)
                        {
                            TempData["ErrorMessage"] = "Adresse introuvable.";
                            return RedirectToAction("Addresses");
                        }
                    }

                    // Si cette adresse est définie comme par défaut, retirer le statut par défaut des autres adresses
                    if (model.EstParDefaut)
                    {
                        var removeDefaultQuery = "UPDATE Adresses SET EstParDefaut = 0 WHERE ClientId = @ClientId AND AdresseId != @AdresseId";
                        using (var removeCommand = new SqlCommand(removeDefaultQuery, connection))
                        {
                            removeCommand.Parameters.AddWithValue("@ClientId", clientId);
                            removeCommand.Parameters.AddWithValue("@AdresseId", model.AdresseId);
                            removeCommand.ExecuteNonQuery();
                        }
                    }

                    var updateQuery = @"UPDATE Adresses 
                                       SET AdresseComplete = @AdresseComplete, Ville = @Ville, 
                                           CodePostal = @CodePostal, Pays = @Pays, EstParDefaut = @EstParDefaut
                                       WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AdresseId", model.AdresseId);
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        command.Parameters.AddWithValue("@AdresseComplete", model.AdresseComplete);
                        command.Parameters.AddWithValue("@Ville", model.Ville);
                        command.Parameters.AddWithValue("@CodePostal", string.IsNullOrEmpty(model.CodePostal) ? (object)DBNull.Value : model.CodePostal);
                        command.Parameters.AddWithValue("@Pays", string.IsNullOrEmpty(model.Pays) ? "Maroc" : model.Pays);
                        command.Parameters.AddWithValue("@EstParDefaut", model.EstParDefaut);
                        command.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] = "L'adresse a été modifiée avec succès.";
                    // Retourner sur l’onglet adresses du profil
                    return RedirectToAction("Profile", new { tab = "adresses" });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de la modification de l'adresse.");
                System.Diagnostics.Debug.WriteLine("Erreur EditAddress POST: " + ex.Message);
            }

            return View(model);
        }

        // POST: UserSpace/DeleteAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteAddress(int id)
        {
            if (!IsUserAuthenticated())
                return Json(new { success = false, message = "Non authentifié." });

            int clientId = (int)Session["ClientId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Vérifier que l'adresse appartient au client
                    var checkQuery = "SELECT COUNT(*) FROM Adresses WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@AdresseId", id);
                        checkCommand.Parameters.AddWithValue("@ClientId", clientId);
                        var exists = (int)checkCommand.ExecuteScalar() > 0;

                        if (!exists)
                        {
                            return Json(new { success = false, message = "Adresse introuvable." });
                        }
                    }

                    // Vérifier si l'adresse est utilisée dans des commandes
                    var checkOrdersQuery = "SELECT COUNT(*) FROM Commandes WHERE AdresseId = @AdresseId";
                    using (var checkOrdersCommand = new SqlCommand(checkOrdersQuery, connection))
                    {
                        checkOrdersCommand.Parameters.AddWithValue("@AdresseId", id);
                        var usedInOrders = (int)checkOrdersCommand.ExecuteScalar() > 0;

                        if (usedInOrders)
                        {
                            return Json(new { success = false, message = "Cette adresse ne peut pas être supprimée car elle est utilisée dans des commandes." });
                        }
                    }

                    var deleteQuery = "DELETE FROM Adresses WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    using (var command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AdresseId", id);
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        command.ExecuteNonQuery();
                    }

                    return Json(new { success = true, message = "L'adresse a été supprimée avec succès." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur DeleteAddress: " + ex.Message);
                return Json(new { success = false, message = "Une erreur est survenue lors de la suppression." });
            }
        }

        // POST: UserSpace/SetDefaultAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SetDefaultAddress(int id)
        {
            if (!IsUserAuthenticated())
                return Json(new { success = false, message = "Non authentifié." });

            int clientId = (int)Session["ClientId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Vérifier que l'adresse appartient au client
                    var checkQuery = "SELECT COUNT(*) FROM Adresses WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@AdresseId", id);
                        checkCommand.Parameters.AddWithValue("@ClientId", clientId);
                        var exists = (int)checkCommand.ExecuteScalar() > 0;

                        if (!exists)
                        {
                            return Json(new { success = false, message = "Adresse introuvable." });
                        }
                    }

                    // Retirer le statut par défaut de toutes les adresses
                    var removeDefaultQuery = "UPDATE Adresses SET EstParDefaut = 0 WHERE ClientId = @ClientId";
                    using (var removeCommand = new SqlCommand(removeDefaultQuery, connection))
                    {
                        removeCommand.Parameters.AddWithValue("@ClientId", clientId);
                        removeCommand.ExecuteNonQuery();
                    }

                    // Définir cette adresse comme par défaut
                    var setDefaultQuery = "UPDATE Adresses SET EstParDefaut = 1 WHERE AdresseId = @AdresseId AND ClientId = @ClientId";
                    using (var setCommand = new SqlCommand(setDefaultQuery, connection))
                    {
                        setCommand.Parameters.AddWithValue("@AdresseId", id);
                        setCommand.Parameters.AddWithValue("@ClientId", clientId);
                        setCommand.ExecuteNonQuery();
                    }

                    return Json(new { success = true, message = "L'adresse par défaut a été mise à jour." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur SetDefaultAddress: " + ex.Message);
                return Json(new { success = false, message = "Une erreur est survenue." });
            }
        }

        // GET: UserSpace/ChangePassword
        public ActionResult ChangePassword()
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            return View();
        }

        // POST: UserSpace/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int utilisateurId = (int)Session["UserId"];

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Récupérer le mot de passe actuel
                    var getPasswordQuery = "SELECT MotDePasse FROM Utilisateurs WHERE UtilisateurId = @UtilisateurId";
                    string storedPassword = null;
                    using (var getCommand = new SqlCommand(getPasswordQuery, connection))
                    {
                        getCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        var result = getCommand.ExecuteScalar();
                        if (result != null)
                        {
                            storedPassword = result.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(storedPassword))
                    {
                        ModelState.AddModelError("", "Erreur lors de la récupération du mot de passe.");
                        return View(model);
                    }

                    // Vérifier le mot de passe actuel
                    bool passwordValid = false;
                    if (storedPassword.Length == 64) // SHA256 hash
                    {
                        passwordValid = PasswordHelper.VerifyPassword(model.CurrentPassword, storedPassword);
                    }
                    else
                    {
                        // Plain text (backward compatibility)
                        passwordValid = model.CurrentPassword == storedPassword;
                    }

                    if (!passwordValid)
                    {
                        ModelState.AddModelError("CurrentPassword", "Le mot de passe actuel est incorrect.");
                        return View(model);
                    }

                    // Mettre à jour le mot de passe
                    string hashedPassword = PasswordHelper.HashPassword(model.NewPassword);
                    var updateQuery = "UPDATE Utilisateurs SET MotDePasse = @MotDePasse WHERE UtilisateurId = @UtilisateurId";
                    using (var updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@MotDePasse", hashedPassword);
                        updateCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        updateCommand.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] = "Votre mot de passe a été modifié avec succès.";
                    // Retourner sur l’onglet sécurité du profil
                    return RedirectToAction("Profile", new { tab = "securite" });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Une erreur est survenue lors de la modification du mot de passe.");
                System.Diagnostics.Debug.WriteLine("Erreur ChangePassword POST: " + ex.Message);
            }

            return View(model);
        }

        // PARTIAL: Security (changer mot de passe) pour onglet dynamique
        [HttpGet]
        public ActionResult SecurityPartial()
        {
            if (!IsUserAuthenticated())
                return new HttpStatusCodeResult(401);

            return PartialView("_SecurityContent", new ChangePasswordViewModel());
        }

        // GET: UserSpace/Preferences
        public ActionResult Preferences()
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            // Pour l'instant, retourner un modèle par défaut
            // Vous pouvez étendre cela pour stocker les préférences dans la base de données
            var model = new AccountPreferencesViewModel
            {
                LanguePreferee = "fr"
            };

            return View(model);
        }

        // POST: UserSpace/Preferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Preferences(AccountPreferencesViewModel model)
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            // Pour l'instant, juste afficher un message de succès
            // Vous pouvez étendre cela pour stocker les préférences dans la base de données
            TempData["SuccessMessage"] = "Vos préférences ont été enregistrées avec succès.";
            return RedirectToAction("Preferences");
        }

        // GET: UserSpace/MesAvis
        public ActionResult MesAvis()
        {
            if (!IsUserAuthenticated())
                return RedirectToLogin();

            int clientId = (int)Session["ClientId"];
            List<AvisProduit> avis = new List<AvisProduit>();

            try
            {
                using (var db = new ECommerceDbContext())
                {
                    avis = db.GetClientAvis(clientId);
                }
                
                // Store DateCreation in ViewBag for sidebar
                if (Session["DateCreation"] != null)
                {
                    ViewBag.DateCreation = Session["DateCreation"];
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement de vos avis.";
                System.Diagnostics.Debug.WriteLine("Erreur MesAvis GET: " + ex.Message);
            }

            return View(avis);
        }
    }
}

