using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;
using E_Commerce_Cooperatives.Models.ViewModels;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Web.Script.Serialization;

namespace E_Commerce_Cooperatives.Controllers
{
    public class AdminController : Controller
    {
        private string connectionString;

        public AdminController()
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

        // GET: Admin
        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }
        

        // GET: Admin/Commandes
        public ActionResult Commandes(string searchTerm = null, string statutFilter = "all", 
            DateTime? dateFrom = null, DateTime? dateTo = null, 
            decimal? montantMin = null, decimal? montantMax = null)
        {
            using (var db = new ECommerceDbContext())
            {
                var commandes = db.GetCommandes(searchTerm, statutFilter, dateFrom, dateTo, montantMin, montantMax);
                var stats = db.GetCommandeStats();

                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatutFilter = statutFilter;
                ViewBag.DateFrom = dateFrom;
                ViewBag.DateTo = dateTo;
                ViewBag.MontantMin = montantMin;
                ViewBag.MontantMax = montantMax;
                ViewBag.Stats = stats;

                return View(commandes);
            }
        }

        // GET: Admin/Commandes/Details/5
        public ActionResult Details(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);
                if (commande == null)
                {
                    return HttpNotFound();
                }
                
                // Create a simplified object for JSON serialization
                var result = new
                {
                    CommandeId = commande.CommandeId,
                    NumeroCommande = commande.NumeroCommande,
                    DateCommande = commande.DateCommande,
                    Statut = commande.Statut,
                    TotalHT = commande.TotalHT,
                    MontantTVA = commande.MontantTVA,
                    FraisLivraison = commande.FraisLivraison,
                    TotalTTC = commande.TotalTTC,
                    Client = new
                    {
                        Nom = commande.Client?.Nom,
                        Prenom = commande.Client?.Prenom,
                        Email = commande.Client?.Email
                    },
                    Adresse = commande.Adresse != null ? new
                    {
                        AdresseComplete = commande.Adresse.AdresseComplete,
                        Ville = commande.Adresse.Ville,
                        CodePostal = commande.Adresse.CodePostal
                    } : null,
                    ModeLivraison = commande.ModeLivraison != null ? new
                    {
                        Nom = commande.ModeLivraison.Nom
                    } : null,
                    Items = commande.Items?.Select(item => new
                    {
                        Produit = new
                        {
                            Nom = item.Produit?.Nom
                        },
                        Quantite = item.Quantite,
                        PrixUnitaire = item.PrixUnitaireTTC, // Prix TTC pour l'affichage
                        TotalLigne = item.TotalLigneTTC // Total TTC pour l'affichage
                    }).ToList()
                };
                
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin/Commandes/UpdateStatus
        [HttpPost]
        public ActionResult UpdateStatus(int commandeId, string nouveauStatut)
        {
            try
            {
                // Empêcher l'annulation via cette méthode - utiliser Cancel() à la place
                if (nouveauStatut == "Annulée")
                {
                    return Json(new { success = false, message = "Pour annuler une commande, veuillez utiliser le bouton 'Annuler la commande' et fournir une raison d'annulation." });
                }

                // Validation des paramètres
                if (commandeId <= 0)
                {
                    return Json(new { success = false, message = "ID de commande invalide" });
                }

                if (string.IsNullOrWhiteSpace(nouveauStatut))
                {
                    return Json(new { success = false, message = "Le statut est obligatoire" });
                }

                // Vérifier que le statut est valide (sauf Annulée)
                var statutsValides = new[] { "Validée", "Préparation", "Expédiée", "Livrée" };
                if (!statutsValides.Contains(nouveauStatut))
                {
                    return Json(new { success = false, message = "Statut invalide" });
                }

                using (var db = new ECommerceDbContext())
                {
                    // Vérifier que la commande existe
                    var commande = db.GetCommandeDetails(commandeId);
                    if (commande == null)
                    {
                        return Json(new { success = false, message = "Commande introuvable" });
                    }

                    // Empêcher de modifier le statut d'une commande annulée
                    if (commande.Statut == "Annulée")
                    {
                        return Json(new { success = false, message = "Impossible de modifier le statut d'une commande annulée" });
                    }

                    var success = db.UpdateCommandeStatut(commandeId, nouveauStatut);
                    if (success)
                    {
                        return Json(new { success = true, message = "Statut mis à jour avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la mise à jour" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // POST: Admin/Commandes/Cancel
        [HttpPost]
        public ActionResult Cancel(int commandeId, string raisonAnnulation)
        {
            try
            {
                // Validation des paramètres
                if (commandeId <= 0)
                {
                    return Json(new { success = false, message = "ID de commande invalide" });
                }

                if (string.IsNullOrWhiteSpace(raisonAnnulation))
                {
                    return Json(new { success = false, message = "La raison d'annulation est obligatoire" });
                }

                if (raisonAnnulation.Trim().Length < 10)
                {
                    return Json(new { success = false, message = "La raison d'annulation doit contenir au moins 10 caractères" });
                }

                using (var db = new ECommerceDbContext())
                {
                    // Vérifier que la commande existe
                    var commande = db.GetCommandeDetails(commandeId);
                    if (commande == null)
                    {
                        return Json(new { success = false, message = "La commande n'a pas été trouvée." });
                    }

                    // Vérifier que la commande n'est pas déjà annulée
                    if (commande.Statut == "Annulée")
                    {
                        return Json(new { success = false, message = "Cette commande est déjà annulée" });
                    }

                    // Vérifier que la commande n'est pas déjà livrée
                    if (commande.Statut == "Livrée")
                    {
                        return Json(new { success = false, message = "Impossible d'annuler une commande déjà livrée" });
                    }

                    // Annuler la commande
                    var success = db.AnnulerCommande(commandeId, raisonAnnulation);
                    if (success)
                    {
                        // Recharger la commande pour avoir les données à jour (statut, date d'annulation, etc.)
                        var commandeAnnulee = db.GetCommandeDetails(commandeId);
                        
                        // Envoyer l'email d'annulation au client
                        try
                        {
                            if (commandeAnnulee != null && commandeAnnulee.Client != null && !string.IsNullOrEmpty(commandeAnnulee.Client.Email))
                            {
                                EmailHelper.SendOrderCancellationEmail(commandeAnnulee, raisonAnnulation);
                            }
                        }
                        catch (Exception emailEx)
                        {
                            // Ne pas faire échouer l'annulation si l'email échoue, mais logger l'erreur
                            System.Diagnostics.Debug.WriteLine("Erreur lors de l'envoi de l'email d'annulation : " + emailEx.Message);
                        }
                        
                        return Json(new { success = true, message = "Commande annulée avec succès" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "La commande n'a pas été trouvée." });
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error in Cancel: " + sqlEx.Message);
                return Json(new { success = false, message = "Erreur de base de données : " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in Cancel: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return Json(new { success = false, message = "Erreur lors de l'annulation : " + ex.Message });
            }
        }
        // GET: Admin/Cooperatives
        public ActionResult Cooperatives(string searchTerm = "")
        {
            try
            {
                var db = new ECommerceDbContext();
                var cooperatives = db.GetCooperativesWithStats()
            .OrderByDescending(c => c.DateCreation)
            .ToList();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    cooperatives = cooperatives.Where(c => 
                        c.Nom.ToLower().Contains(searchTerm.ToLower()) || 
                        c.Ville.ToLower().Contains(searchTerm.ToLower())
                    ).ToList();
                }

                ViewBag.SearchTerm = searchTerm;
                return View(cooperatives);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans Cooperatives: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors du chargement des coopératives";
                return View(new List<Cooperative>());
            }
        }

        [HttpGet]
        public JsonResult GetCooperative(int id)
        {
            try
            {
                var db = new ECommerceDbContext();
                var c = db.GetCooperative(id);
                if (c == null) return Json(new { success = false, message = "Coopérative non trouvée" }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, cooperative = c }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjouterCooperative(Cooperative coop, System.Web.HttpPostedFileBase logoFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (logoFile != null && logoFile.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(logoFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/cooperatives/"), fileName);
                        
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);

                        logoFile.SaveAs(path);
                        coop.Logo = "~/Content/images/cooperatives/" + fileName;
                    }

                    coop.DateCreation = DateTime.Now;
                    db.AddCooperative(coop);

                    return Json(new { success = true, message = "Coopérative ajoutée avec succès" });
                }
                return Json(new { success = false, message = "Données invalides" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ModifierCooperative(Cooperative coop, System.Web.HttpPostedFileBase logoFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existing = db.GetCooperative(coop.CooperativeId);
                    if (existing == null) return Json(new { success = false, message = "Coopérative non trouvée" });

                    if (logoFile != null && logoFile.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(logoFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/cooperatives/"), fileName);
                        
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);

                        logoFile.SaveAs(path);
                        coop.Logo = "~/Content/images/cooperatives/" + fileName;
                    }
                    else
                    {
                        coop.Logo = existing.Logo;
                    }

                    db.UpdateCooperative(coop);
                    return Json(new { success = true, message = "Coopérative modifiée avec succès" });
                }
                return Json(new { success = false, message = "Données invalides" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ToggleStatutCooperative(int id)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Récupérer le statut actuel
                            bool actuelStatut = false;
                            using (var cmdStatus = new SqlCommand("SELECT EstActive FROM Cooperatives WHERE CooperativeId = @id", connection, transaction))
                            {
                                cmdStatus.Parameters.AddWithValue("@id", id);
                                var result = cmdStatus.ExecuteScalar();
                                if (result == null) return Json(new { success = false, message = "Coopérative non trouvée" });
                                actuelStatut = (bool)result;
                            }

                            bool nouveauStatut = !actuelStatut;

                            // 2. Mettre à jour le statut de la coopérative
                            using (var cmdUpdateCoop = new SqlCommand("UPDATE Cooperatives SET EstActive = @nouveauStatut WHERE CooperativeId = @id", connection, transaction))
                            {
                                cmdUpdateCoop.Parameters.AddWithValue("@nouveauStatut", nouveauStatut);
                                cmdUpdateCoop.Parameters.AddWithValue("@id", id);
                                cmdUpdateCoop.ExecuteNonQuery();
                            }

                            int productsAffected = 0;
                            // 3. Si désactivation, désactiver aussi tous les produits
                            if (!nouveauStatut)
                            {
                                var updateProductsQuery = @"UPDATE Produits 
                                                           SET EstDisponible = 0, 
                                                               DateModification = GETDATE() 
                                                           WHERE CooperativeId = @CooperativeId";
                                
                                using (var cmdProducts = new SqlCommand(updateProductsQuery, connection, transaction))
                                {
                                    cmdProducts.Parameters.AddWithValue("@CooperativeId", id);
                                    productsAffected = cmdProducts.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            string action = nouveauStatut ? "activée" : "désactivée";
                            string message = $"Coopérative {action} avec succès";
                            if (!nouveauStatut && productsAffected > 0)
                            {
                                message += $" et {productsAffected} produit(s) rendu(s) indisponible(s).";
                            }

                            return Json(new { success = true, message = message });
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur lors du changement de statut : " + ex.Message });
            }
        }


        // GET: Admin/PrintInvoice/5
        public ActionResult PrintInvoice(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);
                if (commande == null)
                {
                    return HttpNotFound();
                }

                try
                {
                    byte[] pdfBytes = E_Commerce_Cooperatives.Helpers.InvoiceHelper.GenerateInvoice(commande);
                    return File(pdfBytes, "application/pdf", $"Facture_{commande.NumeroCommande}.pdf");
                }
                catch (Exception ex)
                {
                    return Content($"Erreur lors de la génération de la facture: {ex.Message}");
                }
            }
        }

        // GET: Admin/PrintDeliverySlip/5
        public ActionResult PrintDeliverySlip(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);
                if (commande == null)
                {
                    return HttpNotFound();
                }

                try
                {
                    byte[] pdfBytes = E_Commerce_Cooperatives.Helpers.DeliverySlipHelper.GenerateDeliverySlip(commande);
                    return File(pdfBytes, "application/pdf", $"Bordereau_{commande.NumeroCommande}.pdf");
                }
                catch (Exception ex)
                {
                    return Content($"Erreur lors de la génération du bordereau: {ex.Message}");
                }
            }
        }
        private readonly ECommerceDbContext db = new ECommerceDbContext();

        // ============================================
        // GESTION DES PRODUITS
        // ============================================

        /// <summary>
        /// Affiche la liste des produits avec filtres et statistiques
        /// </summary>
        public ActionResult Produits(string searchTerm = "", string categorieFilter = "all", string statutFilter = "all")
        {
            try
            {
                // Récupérer tous les produits
                var produits = db.Produits.ToList();
                
                // Charger les relations manuellement
                var categories = db.Categories.ToList();
                var cooperatives = db.Cooperatives.ToList();
                
                foreach (var p in produits)
                {
                    if (p.CategorieId.HasValue)
                        p.Categorie = categories.FirstOrDefault(c => c.CategorieId == p.CategorieId.Value);
                    if (p.CooperativeId.HasValue)
                        p.Cooperative = cooperatives.FirstOrDefault(c => c.CooperativeId == p.CooperativeId.Value);
                }

                // Appliquer les filtres
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    produits = produits.Where(p =>
                        p.Nom.ToLower().Contains(searchTerm.ToLower()) ||
                        (p.Cooperative != null && p.Cooperative.Nom.ToLower().Contains(searchTerm.ToLower()))
                    ).ToList();
                }

                if (categorieFilter != "all" && int.TryParse(categorieFilter, out int catId))
                {
                    produits = produits.Where(p => p.CategorieId == catId).ToList();
                }

                if (statutFilter != "all")
                {
                    switch (statutFilter)
                    {
                        case "disponible":
                            produits = produits.Where(p => p.EstDisponible).ToList();
                            break;
                        case "vedette":
                            produits = produits.Where(p => p.EstEnVedette).ToList();
                            break;
                        case "stock-faible":
                            produits = produits.Where(p => p.StockTotal <= p.SeuilAlerte).ToList();
                            break;
                    }
                }

                // Trier par date d'ajout (plus récent au début)
                produits = produits.OrderByDescending(p => p.DateCreation).ToList();

                // Calculer les statistiques
                var stats = new Dictionary<string, int>
                {
                    { "Total", produits.Count },
                    { "Disponibles", produits.Count(p => p.EstDisponible) },
                    { "EnVedette", produits.Count(p => p.EstEnVedette) },
                    { "StockFaible", produits.Count(p => p.StockTotal <= p.SeuilAlerte) }
                };

                // Passer les données à la vue
                ViewBag.Stats = stats;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.CategorieFilter = categorieFilter;
                ViewBag.StatutFilter = statutFilter;

                // For Modals
                ViewBag.Categories = categories;
                ViewBag.Cooperatives = cooperatives;

                return View("Admin_Produit", produits);
            }
            catch (Exception ex)
            {
                // Logger l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur dans Produits: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors du chargement des produits";
                return View("Admin_Produit", new List<Produit>());
            }
        }

        [HttpGet]
        public JsonResult GetProduit(int id)
        {
            try
            {
                var p = db.FindProduit(id);
                if (p == null) return Json(new { success = false, message = "Produit non trouvé" }, JsonRequestBehavior.AllowGet);

                var variantes = db.GetVariantesProduit(id);

                return Json(new
                {
                    success = true,
                    produit = new
                    {
                        p.ProduitId,
                        p.Nom,
                        p.Description,
                        p.Prix,
                        p.CategorieId,
                        p.CooperativeId,
                        p.StockTotal,
                        p.SeuilAlerte,
                        p.EstDisponible,
                        p.EstEnVedette,
                        p.EstNouveau,
                        p.ImageUrl
                    },
                    variantes = variantes.Select(v => new {
                        v.VarianteId,
                        v.Taille,
                        v.Couleur,
                        v.Stock,
                        v.PrixSupplementaire,
                        v.SKU,
                        v.EstDisponible
                    }).ToList(),
                    images = db.GetImagesProduit(id).Select(img => new {
                        img.ImageId,
                        img.UrlImage,
                        img.EstPrincipale
                    }).ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Affiche le formulaire d'ajout de produit
        /// </summary>
        public ActionResult AjouterProduit()
        {
            try
            {
                ViewBag.Categories = db.Categories.ToList();
                ViewBag.Cooperatives = db.Cooperatives.ToList();
                return View(new Produit());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans AjouterProduit GET: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
                return RedirectToAction("Produits");
            }
        }

        /// <summary>
        /// Traite l'ajout d'un nouveau produit
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjouterProduit(Produit produit, System.Web.HttpPostedFileBase imageFile, IEnumerable<System.Web.HttpPostedFileBase> secondaryImages, string variantesJson)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Gestion de l'image
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(imageFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/produits/"), fileName);
                        
                        // Créer le répertoire s'il n'existe pas
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);

                        imageFile.SaveAs(path);
                        produit.ImageUrl = "~/Content/images/produits/" + fileName;
                    }

                    produit.DateCreation = DateTime.Now;
                    db.ProduitsSet.Add(produit);
                    db.SaveChanges();

                    // Gestion des images secondaires
                    if (secondaryImages != null)
                    {
                        foreach (var file in secondaryImages)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
                                string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/produits/"), fileName);
                                
                                file.SaveAs(path);
                                
                                var img = new ImageProduit
                                {
                                    ProduitId = produit.ProduitId,
                                    UrlImage = "~/Content/images/produits/" + fileName,
                                    EstPrincipale = false
                                };
                                db.AddImageProduit(img);
                            }
                        }
                    }

                    if (Request.IsAjaxRequest())
                    {
                        // Gestion des variantes
                        if (!string.IsNullOrEmpty(variantesJson))
                        {
                            var serializer = new JavaScriptSerializer();
                            var variantes = serializer.Deserialize<List<Variante>>(variantesJson);
                            if (variantes != null)
                            {
                                foreach (var v in variantes)
                                {
                                    v.ProduitId = produit.ProduitId;
                                    db.AddVariante(v);
                                }
                            }
                        }

                        return Json(new { success = true, message = "Produit ajouté avec succès" });
                    }

                    TempData["SuccessMessage"] = "Produit ajouté avec succès";
                    return RedirectToAction("Produits");
                }

                if (Request.IsAjaxRequest())
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, errors = errors });
                }

                ViewBag.Categories = db.Categories.ToList();
                ViewBag.Cooperatives = db.Cooperatives.ToList();
                return View(produit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans AjouterProduit POST: {ex.Message}");
                
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Erreur lors de l'ajout du produit: " + ex.Message });
                }

                TempData["ErrorMessage"] = "Erreur lors de l'ajout du produit";
                ViewBag.Categories = db.Categories.ToList();
                ViewBag.Cooperatives = db.Cooperatives.ToList();
                return View(produit);
            }
        }

        /// <summary>
        /// Affiche les détails d'un produit
        /// </summary>
        public ActionResult DetailsProduit(int id)
        {
            try
            {
                var produit = db.Produits.FirstOrDefault(p => p.ProduitId == id);
                
                if (produit != null)
                {
                    // Charger les relations manuellement
                    if (produit.CategorieId.HasValue)
                        produit.Categorie = db.Categories.FirstOrDefault(c => c.CategorieId == produit.CategorieId.Value);
                    if (produit.CooperativeId.HasValue)
                        produit.Cooperative = db.Cooperatives.FirstOrDefault(c => c.CooperativeId == produit.CooperativeId.Value);
                }

                if (produit == null)
                {
                    TempData["ErrorMessage"] = "Produit non trouvé";
                    return RedirectToAction("Produits");
                }

                return View(produit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans DetailsProduit: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors du chargement des détails";
                return RedirectToAction("Produits");
            }
        }

        /// <summary>
        /// Affiche le formulaire de modification de produit
        /// </summary>
        public ActionResult ModifierProduit(int id)
        {
            try
            {
                var produit = db.ProduitsSet.Find(id);

                if (produit == null)
                {
                    TempData["ErrorMessage"] = "Produit non trouvé";
                    return RedirectToAction("Produits");
                }

                ViewBag.Categories = db.Categories.Where(c => c.EstActive).ToList();
                ViewBag.Cooperatives = db.Cooperatives.Where(c => c.EstActive).ToList();

                return View(produit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans ModifierProduit GET: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors du chargement du produit";
                return RedirectToAction("Produits");
            }
        }

        /// <summary>
        /// Traite la modification d'un produit
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ModifierProduit(Produit produit, System.Web.HttpPostedFileBase imageFile, IEnumerable<System.Web.HttpPostedFileBase> secondaryImages, string deletedImageIds, string variantesJson)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var produitExistant = db.ProduitsSet.Find(produit.ProduitId);

                    if (produitExistant == null)
                    {
                        TempData["ErrorMessage"] = "Produit non trouvé";
                        return RedirectToAction("Produits");
                    }

                    // Gestion de l'image
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(imageFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/produits/"), fileName);
                        
                        // Créer le répertoire s'il n'existe pas
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);

                        imageFile.SaveAs(path);
                        
                        // Optionnel : Supprimer l'ancienne image si elle existe et n'est pas une image par défaut
                        
                        produitExistant.ImageUrl = "~/Content/images/produits/" + fileName;
                    }
                    else if (!string.IsNullOrEmpty(produit.ImageUrl))
                    {
                        // Si l'utilisateur a modifié l'URL manuellement ou si elle est passée en masqué
                        produitExistant.ImageUrl = produit.ImageUrl;
                    }

                    // Mettre à jour les propriétés
                    produitExistant.Nom = produit.Nom;
                    produitExistant.Description = produit.Description;
                    produitExistant.Prix = produit.Prix;
                    produitExistant.CategorieId = produit.CategorieId;
                    produitExistant.CooperativeId = produit.CooperativeId;
                    produitExistant.StockTotal = produit.StockTotal;
                    produitExistant.SeuilAlerte = produit.SeuilAlerte;
                    produitExistant.EstDisponible = produit.EstDisponible;
                    produitExistant.EstEnVedette = produit.EstEnVedette;
                    produitExistant.EstNouveau = produit.EstNouveau;
                    produitExistant.DateModification = DateTime.Now;
                    
                    db.UpdateProduit(produitExistant);
                    db.SaveChanges();

                    // Gestion des images secondaires (Granulaire)
                    
                    // 1. Suppressions
                    if (!string.IsNullOrEmpty(deletedImageIds))
                    {
                        var idsToDelete = deletedImageIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(id => int.Parse(id))
                                                       .ToList();
                        foreach (var id in idsToDelete)
                        {
                            db.DeleteImageProduit(id);
                        }
                    }

                    // 2. Ajouts
                    if (secondaryImages != null)
                    {
                        foreach (var file in secondaryImages)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
                                string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/produits/"), fileName);
                                
                                file.SaveAs(path);
                                
                                var img = new ImageProduit
                                {
                                    ProduitId = produit.ProduitId,
                                    UrlImage = "~/Content/images/produits/" + fileName,
                                    EstPrincipale = false
                                };
                                db.AddImageProduit(img);
                            }
                        }
                    }

                    if (Request.IsAjaxRequest())
                    {
                        // Gestion des variantes
                        if (variantesJson != null)
                        {
                            var serializer = new JavaScriptSerializer();
                            var variantes = serializer.Deserialize<List<Variante>>(variantesJson);
                            
                            // Récupérer les variantes existantes
                            var variantesExistantes = db.GetVariantesProduit(produit.ProduitId);
                            
                            if (variantes != null)
                            {
                                foreach (var v in variantes)
                                {
                                    v.ProduitId = produit.ProduitId;
                                    if (v.VarianteId > 0)
                                    {
                                        db.UpdateVariante(v);
                                    }
                                    else
                                    {
                                        db.AddVariante(v);
                                    }
                                }
                                
                                // Supprimer les variantes qui ne sont plus dans la liste
                                var idsIdsRestants = variantes.Where(v => v.VarianteId > 0).Select(v => v.VarianteId).ToList();
                                foreach (var ve in variantesExistantes)
                                {
                                    if (!idsIdsRestants.Contains(ve.VarianteId))
                                    {
                                        db.DeleteVariante(ve.VarianteId);
                                    }
                                }
                            }
                            else
                            {
                                // Si la liste est vide/nulle, supprimer toutes les variantes? 
                                // On va dire que si c'est null on touche à rien, mais si c'est "[]" on supprime tout.
                                // Le désérialiseur donnera une liste vide pour "[]".
                            }
                        }

                        return Json(new { success = true, message = "Produit modifié avec succès" });
                    }

                    TempData["SuccessMessage"] = "Produit modifié avec succès";
                    return RedirectToAction("Produits");
                }

                if (Request.IsAjaxRequest())
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, errors = errors });
                }

                ViewBag.Categories = db.Categories.Where(c => c.EstActive).ToList();
                ViewBag.Cooperatives = db.Cooperatives.Where(c => c.EstActive).ToList();

                return View(produit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans ModifierProduit POST: {ex.Message}");

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Erreur lors de la modification du produit: " + ex.Message });
                }

                TempData["ErrorMessage"] = "Erreur lors de la modification du produit";

                ViewBag.Categories = db.Categories.Where(c => c.EstActive).ToList();
                ViewBag.Cooperatives = db.Cooperatives.Where(c => c.EstActive).ToList();

                return View(produit);
            }
        }

        [HttpPost]
        public JsonResult SupprimerProduit(int id)
        {
            try
            {
                var p = db.ProduitsSet.Find(id);
                if (p == null) return Json(new { success = false, message = "Produit non trouvé" });

                // Soft delete: désactiver le produit au lieu de le supprimer
                p.EstDisponible = false;
                db.SaveChanges();
                
                return Json(new { success = true, message = "Produit désactivé avec succès" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Supprime un produit (AJAX)
        /// </summary>
        //[HttpPost]
        //public JsonResult SupprimerProduit(int id)
        //{
        //    try
        //    {
        //        var produit = db.ProduitsSet.Find(id);

        //        if (produit == null)
        //        {
        //            return Json(new { success = false, message = "Produit non trouvé" });
        //        }

        //        // Vérifier si le produit est dans des commandes
        //        var commandesAvecProduit = db.CommandeItems.Any(ci => ci.ProduitId == id);

        //        if (commandesAvecProduit)
        //        {
        //            return Json(new { success = false, message = "Impossible de supprimer : le produit est présent dans des commandes" });
        //        }

        //        db.ProduitsSet.Remove(produit);
        //        db.SaveChanges();

        //        return Json(new { success = true, message = "Produit supprimé avec succès" });
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Erreur dans SupprimerProduit: {ex.Message}");
        //        return Json(new { success = false, message = "Erreur lors de la suppression" });
        //    }
        //}

        /// <summary>
        /// Met à jour le statut d'un produit (AJAX)
        /// </summary>
        [HttpPost]
        public JsonResult UpdateStatutProduit(int id, bool disponible)
        {
            try
            {
                var produit = db.ProduitsSet.Find(id);

                if (produit == null)
                {
                    return Json(new { success = false, message = "Produit non trouvé" });
                }

                produit.EstDisponible = disponible;
                produit.DateModification = DateTime.Now;
                db.UpdateProduit(produit);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans UpdateStatutProduit: {ex.Message}");
                return Json(new { success = false, message = "Erreur lors de la mise à jour" });
            }
        }

        public ActionResult Livraison(int page = 1)
        {
            using (var db = new ECommerceDbContext())
            {
                var modes = db.GetModesLivraison();
                var zonesPaged = db.GetZonesLivraisonPaged(page, 10);
                var stats = db.GetLivraisonStats();

                ViewBag.Stats = stats;
                ViewBag.ZonesPaged = zonesPaged;
                ViewBag.Zones = zonesPaged.Items;
                ViewBag.CurrentPage = page;

                return View(modes);
            }
        }

        // --- Delivery Modes ---

        [HttpGet]
        public ActionResult GetModeLivraison(int id)
        {
            try
            {
                using (var db = new ECommerceDbContext())
                {
                    var mode = db.GetModeLivraison(id);
                    if (mode == null) return Json(new { success = false, message = "Mode introuvable" }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = true, mode = mode }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CreateModeLivraison(ModeLivraison mode)
        {
            try
            {
                ModelState.Remove("ModeLivraisonId");
                ModelState.Remove("DateCreation");

                if (ModelState.IsValid)
                {
                    mode.DateCreation = DateTime.Now;
                    using (var db = new ECommerceDbContext())
                    {
                        db.CreateModeLivraison(mode);
                        return Json(new { success = true, message = "Mode de livraison ajouté avec succès" });
                    }
                }
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Données invalides: " + errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdateModeLivraison(ModeLivraison mode)
        {
            try
            {
                ModelState.Remove("DateCreation");

                if (ModelState.IsValid)
                {
                    using (var db = new ECommerceDbContext())
                    {
                        db.UpdateModeLivraison(mode);
                        return Json(new { success = true, message = "Mode de livraison mis à jour" });
                    }
                }
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Données invalides: " + errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteModeLivraison(int id)
        {
            try
            {
                using (var db = new ECommerceDbContext())
                {
                    db.DeleteModeLivraison(id);
                    return Json(new { success = true, message = "Mode de livraison supprimé" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // --- Delivery Zones ---

        [HttpGet]
        public ActionResult GetZoneLivraison(int id)
        {
            try
            {
                using (var db = new ECommerceDbContext())
                {
                    var zone = db.GetZoneLivraison(id);
                    if (zone == null) return Json(new { success = false, message = "Zone introuvable" }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = true, zone = zone }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CreateZoneLivraison(ZoneLivraison zone)
        {
            try
            {
                ModelState.Remove("ZoneLivraisonId");
                ModelState.Remove("DateCreation");

                if (ModelState.IsValid)
                {
                    zone.DateCreation = DateTime.Now;
                    using (var db = new ECommerceDbContext())
                    {
                        db.CreateZoneLivraison(zone);
                        return Json(new { success = true, message = "Zone de livraison ajoutée" });
                    }
                }
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Données invalides: " + errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdateZoneLivraison(ZoneLivraison zone)
        {
            try
            {
                ModelState.Remove("DateCreation");

                if (ModelState.IsValid)
                {
                    using (var db = new ECommerceDbContext())
                    {
                        db.UpdateZoneLivraison(zone);
                        return Json(new { success = true, message = "Zone de livraison mise à jour" });
                    }
                }
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Données invalides: " + errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteZoneLivraison(int id)
        {
            try
            {
                using (var db = new ECommerceDbContext())
                {
                    db.DeleteZoneLivraison(id);
                    return Json(new { success = true, message = "Zone de livraison supprimée" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // ============================================
        // GESTION DES CATEGORIES
        // ============================================

        public ActionResult Categories(string searchTerm = "")
        {
            try
            {
                var categories = db.GetCategoriesWithStats()
                    .OrderByDescending(c => c.DateCreation)
                    .ToList();
                
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    categories = categories.Where(c => 
                        c.Nom.ToLower().Contains(searchTerm) || 
                        (!string.IsNullOrEmpty(c.Description) && c.Description.ToLower().Contains(searchTerm))
                    ).ToList();
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.TotalCategories = categories.Count;
                ViewBag.ActiveCategories = categories.Count(c => c.EstActive);
                ViewBag.TotalProductsInCategory = categories.Sum(c => c.NombreProduits);

                return View(categories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans Categories: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors du chargement des catégories";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Récupère les données d'une catégorie pour le modal d'édition
        /// </summary>
        [HttpGet]
        public JsonResult GetCategorie(int id)
        {
            try
            {
                var c = db.GetCategorie(id);
                if (c == null) return Json(new { success = false, message = "Catégorie non trouvée" }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    categorie = new
                    {
                        c.CategorieId,
                        c.Nom,
                        c.Description,
                        c.ImageUrl,
                        c.EstActive
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult AjouterCategorie()
        {
            return View(new Categorie { EstActive = true }); // Active par défaut
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjouterCategorie(Categorie categorie, System.Web.HttpPostedFileBase imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(imageFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/categories/"), fileName);
                        
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);

                        imageFile.SaveAs(path);
                        categorie.ImageUrl = "~/Content/images/categories/" + fileName;
                    }

                    categorie.DateCreation = DateTime.Now;
                    db.AddCategorie(categorie);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Catégorie ajoutée avec succès" });
                    }

                    TempData["SuccessMessage"] = "Catégorie ajoutée avec succès";
                    return RedirectToAction("Categories");
                }

                if (Request.IsAjaxRequest())
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Données invalides", errors = errors });
                }
                return View(categorie);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans AjouterCategorie: {ex.Message}");
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Erreur lors de l'ajout: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Erreur lors de l'ajout";
                return View(categorie);
            }
        }

        public ActionResult ModifierCategorie(int id)
        {
            var categorie = db.GetCategorie(id);
            if (categorie == null)
            {
                TempData["ErrorMessage"] = "Catégorie non trouvée";
                return RedirectToAction("Categories");
            }
            return View(categorie);
        }

        [HttpPost]
        public JsonResult SupprimerCategorie(int id)
        {
            try
            {
                var c = db.GetCategorie(id);
                if (c == null) return Json(new { success = false, message = "Catégorie non trouvée" });

                // Optionnel: vérifier s'il y a des produits
                if (db.Produits.Any(p => p.CategorieId == id))
                {
                    return Json(new { success = false, message = "Impossible de supprimer une catégorie contenant des produits" });
                }

                db.DeleteCategorie(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ModifierCategorie(Categorie categorie, System.Web.HttpPostedFileBase imageFile)
        {
            try
            {
                // DateCreation n'est pas dans le formulaire, on l'ignore pour la validation
                ModelState.Remove("DateCreation");

                if (ModelState.IsValid)
                {
                    var existing = db.GetCategorie(categorie.CategorieId);
                    if (existing == null)
                    {
                        if (Request.IsAjaxRequest())
                            return Json(new { success = false, message = "Catégorie non trouvée" });

                        TempData["ErrorMessage"] = "Catégorie non trouvée";
                        return RedirectToAction("Categories");
                    }

                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(imageFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/categories/"), fileName);
                        
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);

                        imageFile.SaveAs(path);
                        categorie.ImageUrl = "~/Content/images/categories/" + fileName;
                    }
                    else
                    {
                        categorie.ImageUrl = existing.ImageUrl; // Garder l'ancienne image
                    }

                    // On s'assure de garder la date de création originale
                    categorie.DateCreation = existing.DateCreation;

                    db.UpdateCategorie(categorie);

                    if (Request.IsAjaxRequest())
                        return Json(new { success = true, message = "Catégorie mise à jour avec succès" });

                    TempData["SuccessMessage"] = "Catégorie mise à jour avec succès";
                    return RedirectToAction("Categories");
                }

                if (Request.IsAjaxRequest())
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation échouée", errors = errors });
                }

                return View(categorie);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans ModifierCategorie: {ex.Message}");
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Erreur lors de la modification: " + ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la modification";
                return View(categorie);
            }
        }

        [HttpPost]
        public JsonResult ToggleCategorieStatus(int id)
        {
            try
            {
                db.ToggleCategorieStatus(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans ToggleCategorieStatus: {ex.Message}");
                return Json(new { success = false, message = "Erreur serveur" });
            }
        }

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var dashboard = new DashboardViewModel();

                // Stats Cards
                dashboard = GetDashboardStats(connection, dashboard);
                
                // Recent Orders
                dashboard.RecentOrders = GetRecentOrders(connection, 4);
                
                // Low Stock Products
                dashboard.LowStockProducts = GetLowStockProducts(connection);
                
                // Best Selling Products
                dashboard.BestSellingProducts = GetBestSellingProducts(connection, 24);
                
                // Cooperatives
                dashboard.Cooperatives = GetCooperatives(connection);

                return View(dashboard);
            }
        }

        // POST: Admin/AddUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddUser(string Prenom, string Nom, string Email, string Telephone, string MotDePasse)
        {
            // Handle anti-forgery token validation errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                if (errors.Any(e => e.Contains("token") || e.Contains("Token") || e.Contains("anti-forgery")))
                {
                    return Json(new { success = false, message = "Erreur de sécurité. Veuillez recharger la page et réessayer." }, JsonRequestBehavior.AllowGet);
                }
            }
            
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Non autorisé" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                if (string.IsNullOrEmpty(Prenom) || string.IsNullOrEmpty(Nom) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(MotDePasse))
                {
                    return Json(new { success = false, message = "Tous les champs obligatoires doivent être remplis." }, JsonRequestBehavior.AllowGet);
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if email exists
                    var checkEmailQuery = "SELECT COUNT(*) FROM Utilisateurs WHERE Email = @Email";
                    using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Email);
                        var emailExists = (int)checkCommand.ExecuteScalar() > 0;

                        if (emailExists)
                        {
                            return Json(new { success = false, message = "Cet email est déjà utilisé." }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Hash password
                    string hashedPassword = E_Commerce_Cooperatives.Models.PasswordHelper.HashPassword(MotDePasse);

                    // Insert user
                    var insertUserQuery = @"INSERT INTO Utilisateurs (Email, MotDePasse, TypeUtilisateur, DateCreation) 
                                           VALUES (@Email, @MotDePasse, 'Client', GETDATE());
                                           SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    int utilisateurId;
                    using (var userCommand = new SqlCommand(insertUserQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@Email", Email);
                        userCommand.Parameters.AddWithValue("@MotDePasse", hashedPassword);
                        utilisateurId = (int)userCommand.ExecuteScalar();
                    }

                    // Insert client
                    var insertClientQuery = @"INSERT INTO Clients (UtilisateurId, Nom, Prenom, Telephone, EstActif, DateCreation) 
                                             VALUES (@UtilisateurId, @Nom, @Prenom, @Telephone, 1, GETDATE());
                                             SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    int clientId;
                    using (var clientCommand = new SqlCommand(insertClientQuery, connection))
                    {
                        clientCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        clientCommand.Parameters.AddWithValue("@Nom", Nom);
                        clientCommand.Parameters.AddWithValue("@Prenom", Prenom);
                        clientCommand.Parameters.AddWithValue("@Telephone", string.IsNullOrEmpty(Telephone) ? (object)DBNull.Value : Telephone);
                        clientId = (int)clientCommand.ExecuteScalar();
                    }

                    return Json(new { success = true, message = "Client ajouté avec succès" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error in AddUser: " + sqlEx.Message);
                return Json(new { success = false, message = "Erreur de base de données: " + sqlEx.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in AddUser: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return Json(new { success = false, message = "Erreur: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Admin/Utilisateurs
        public ActionResult Utilisateurs(string searchTerm = "", string statusFilter = "all", int page = 1, int pageSize = 6)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Stats cards
                var statsQuery = @"
                    SELECT 
                        COUNT(*) as TotalUsers,
                        SUM(CASE WHEN c.EstActif = 1 THEN 1 ELSE 0 END) as ActiveUsers,
                        SUM(CASE WHEN u.DateCreation >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 1 ELSE 0 END) as ThisMonthUsers,
                        (SELECT COUNT(*) FROM Commandes) as TotalOrders
                    FROM Utilisateurs u
                    LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                    WHERE u.TypeUtilisateur = 'Client';";

                using (var statsCmd = new SqlCommand(statsQuery, connection))
                using (var reader = statsCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ViewBag.CardTotalUsers = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        ViewBag.CardActiveUsers = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        ViewBag.CardThisMonth = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        ViewBag.CardTotalOrders = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    }
                }

                var users = GetUsersList(connection, searchTerm, statusFilter, page, pageSize, out int totalCount);

                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return View(users);
            }
        }

        // GET: Admin/Utilisateurs/Details/5
        public ActionResult UserDetails(int id)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var user = GetUserDetails(connection, id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                return View(user);
            }
        }

        // POST: Admin/Utilisateurs/ToggleStatus
        [HttpPost]
        public ActionResult ToggleUserStatus(int userId, bool isActive)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Non autorisé" });
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Mettre à jour le statut du client
                            var updateQuery = @"UPDATE Clients 
                                              SET EstActif = @EstActif 
                                              WHERE ClientId = @ClientId";
                            
                            using (var command = new SqlCommand(updateQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@EstActif", isActive);
                                command.Parameters.AddWithValue("@ClientId", userId);
                                command.ExecuteNonQuery();
                            }

                            string message = isActive ? "Compte activé avec succès" : "Compte désactivé avec succès";

                            // 2. Si on DÉSACTIVE, on annule TOUTES les commandes qui ne sont pas déjà annulées ou livrées
                            // On exclut explicitement 'Livrée' pour ne pas fausser l'historique des ventes réussies
                            if (!isActive)
                            {
                                var updateOrdersQuery = @"UPDATE Commandes 
                                                        SET Statut = 'Annulée', 
                                                            DateAnnulation = GETDATE(), 
                                                            RaisonAnnulation = 'Compte client désactivé par l''administrateur' 
                                                        WHERE ClientId = @ClientId 
                                                        AND Statut NOT IN ('Annulée', 'Livrée')"; 

                                int ordersAffected = 0;
                                using (var cmdOrders = new SqlCommand(updateOrdersQuery, connection, transaction))
                                {
                                    cmdOrders.Parameters.AddWithValue("@ClientId", userId);
                                    ordersAffected = cmdOrders.ExecuteNonQuery();
                                }

                                if (ordersAffected > 0)
                                {
                                    message += $" et {ordersAffected} commande(s) ont été annulée(s).";
                                }
                                else 
                                {
                                     // Pour le debug : si 0 commandes touchées, c'est peut-être qu'elles sont toutes déjà annulées ou livrées
                                     message += " (Aucune commande en cours à annuler).";
                                }
                            }

                            transaction.Commit();
                            return Json(new { success = true, message = message });
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur: " + ex.Message });
            }
        }

        // GET: Admin/GetUserDetailsJson/5 (JSON)
        [HttpGet]
        public JsonResult GetUserDetailsJson(int id)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Non autorisé" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var user = GetUserDetails(connection, id);
                    if (user == null)
                    {
                        return Json(new { success = false, message = "Utilisateur introuvable" }, JsonRequestBehavior.AllowGet);
                    }

                    // Create a serializable object with proper date formatting
                    var userData = new
                    {
                        ClientId = user.ClientId,
                        UtilisateurId = user.UtilisateurId,
                        Prenom = user.Prenom ?? "",
                        Nom = user.Nom ?? "",
                        Email = user.Email ?? "",
                        Telephone = user.Telephone ?? "",
                        DateNaissance = user.DateNaissance.HasValue ? user.DateNaissance.Value.ToString("yyyy-MM-dd") : (string)null,
                        EstActif = user.EstActif,
                        DateCreation = user.DateCreation.ToString("yyyy-MM-ddTHH:mm:ss"),
                        DerniereConnexion = user.DerniereConnexion.HasValue ? user.DerniereConnexion.Value.ToString("yyyy-MM-ddTHH:mm:ss") : (string)null,
                        OrderCount = user.OrderCount,
                        TotalSpent = user.TotalSpent
                    };

                    return Json(new { success = true, user = userData }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetUserDetailsJson: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return Json(new { success = false, message = "Erreur: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin/UpdateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateUser(int ClientId, string Prenom, string Nom, string Email, string Telephone)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Non autorisé" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                if (string.IsNullOrEmpty(Prenom) || string.IsNullOrEmpty(Nom) || string.IsNullOrEmpty(Email))
                {
                    return Json(new { success = false, message = "Les champs Prénom, Nom et Email sont obligatoires." }, JsonRequestBehavior.AllowGet);
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if email exists for another user
                    var checkEmailQuery = @"SELECT COUNT(*) FROM Utilisateurs u
                                           INNER JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                                           WHERE u.Email = @Email AND c.ClientId != @ClientId";
                    using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Email);
                        checkCommand.Parameters.AddWithValue("@ClientId", ClientId);
                        var emailExists = (int)checkCommand.ExecuteScalar() > 0;

                        if (emailExists)
                        {
                            return Json(new { success = false, message = "Cet email est déjà utilisé par un autre utilisateur." }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Get UtilisateurId for this ClientId
                    var getUserIdQuery = "SELECT UtilisateurId FROM Clients WHERE ClientId = @ClientId";
                    int utilisateurId;
                    using (var getUserIdCmd = new SqlCommand(getUserIdQuery, connection))
                    {
                        getUserIdCmd.Parameters.AddWithValue("@ClientId", ClientId);
                        var result = getUserIdCmd.ExecuteScalar();
                        if (result == null)
                        {
                            return Json(new { success = false, message = "Client introuvable" }, JsonRequestBehavior.AllowGet);
                        }
                        utilisateurId = (int)result;
                    }

                    // Update Utilisateur
                    var updateUserQuery = @"UPDATE Utilisateurs 
                                           SET Email = @Email 
                                           WHERE UtilisateurId = @UtilisateurId";
                    using (var userCommand = new SqlCommand(updateUserQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@Email", Email);
                        userCommand.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                        userCommand.ExecuteNonQuery();
                    }

                    // Update Client
                    var updateClientQuery = @"UPDATE Clients 
                                             SET Nom = @Nom, 
                                                 Prenom = @Prenom, 
                                                 Telephone = @Telephone
                                             WHERE ClientId = @ClientId";
                    using (var clientCommand = new SqlCommand(updateClientQuery, connection))
                    {
                        clientCommand.Parameters.AddWithValue("@Nom", Nom);
                        clientCommand.Parameters.AddWithValue("@Prenom", Prenom);
                        clientCommand.Parameters.AddWithValue("@Telephone", string.IsNullOrEmpty(Telephone) ? (object)DBNull.Value : Telephone);
                        clientCommand.Parameters.AddWithValue("@ClientId", ClientId);
                        clientCommand.ExecuteNonQuery();
                    }

                    return Json(new { success = true, message = "Client modifié avec succès" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error in UpdateUser: " + sqlEx.Message);
                return Json(new { success = false, message = "Erreur de base de données: " + sqlEx.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in UpdateUser: " + ex.Message);
                return Json(new { success = false, message = "Erreur: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Admin/Utilisateurs/GetStats
        [HttpGet]
        public ActionResult GetUserStats(string period = "month")
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var stats = GetUserStatistics(connection, period);
                return Json(new
                {
                    TotalUsers = stats.TotalUsers,
                    ActiveUsers = stats.ActiveUsers,
                    InactiveUsers = stats.InactiveUsers,
                    NewUsers = stats.NewUsers,
                    ActiveThisPeriod = stats.ActiveThisPeriod,
                    ActivityRate = stats.ActivityRate
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper Methods
        private UserStatisticsViewModel GetUserStatistics(SqlConnection connection, string period = "month")
        {
            DateTime startDate;
            switch (period.ToLower())
            {
                case "day":
                    startDate = DateTime.Now.Date;
                    break;
                case "week":
                    startDate = DateTime.Now.AddDays(-7);
                    break;
                case "month":
                    startDate = DateTime.Now.AddMonths(-1);
                    break;
                default:
                    startDate = DateTime.Now.AddMonths(-1);
                    break;
            }

            var query = @"SELECT 
                            COUNT(*) as TotalUsers,
                            SUM(CASE WHEN c.EstActif = 1 THEN 1 ELSE 0 END) as ActiveUsers,
                            SUM(CASE WHEN c.EstActif = 0 THEN 1 ELSE 0 END) as InactiveUsers,
                            SUM(CASE WHEN u.DateCreation >= @StartDate THEN 1 ELSE 0 END) as NewUsers,
                            SUM(CASE WHEN c.DerniereConnexion >= @StartDate THEN 1 ELSE 0 END) as ActiveThisPeriod
                         FROM Utilisateurs u
                         LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                         WHERE u.TypeUtilisateur = 'Client'";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int totalUsers = reader.GetInt32(0);
                        int activeUsers = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        int inactiveUsers = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        int newUsers = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        int activeThisPeriod = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                        double activityRate = totalUsers > 0 ? (double)activeThisPeriod / totalUsers * 100 : 0;

                        return new UserStatisticsViewModel
                        {
                            TotalUsers = totalUsers,
                            ActiveUsers = activeUsers,
                            InactiveUsers = inactiveUsers,
                            NewUsers = newUsers,
                            ActiveThisPeriod = activeThisPeriod,
                            ActivityRate = Math.Round(activityRate, 2)
                        };
                    }
                }
            }

            return new UserStatisticsViewModel
            {
                TotalUsers = 0,
                ActiveUsers = 0,
                InactiveUsers = 0,
                NewUsers = 0,
                ActiveThisPeriod = 0,
                ActivityRate = 0.0
            };
        }

        private List<UserEvolutionViewModel> GetUserEvolutionData(SqlConnection connection)
        {
            var evolutionData = new List<UserEvolutionViewModel>();
            var startDate = DateTime.Now.AddMonths(-6);

            var query = @"SELECT 
                            CAST(u.DateCreation AS DATE) as Date,
                            COUNT(*) as Count
                         FROM Utilisateurs u
                         WHERE u.TypeUtilisateur = 'Client' 
                           AND u.DateCreation >= @StartDate
                         GROUP BY CAST(u.DateCreation AS DATE)
                         ORDER BY Date";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        evolutionData.Add(new UserEvolutionViewModel
                        {
                            Date = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                            Count = reader.GetInt32(1)
                        });
                    }
                }
            }

            return evolutionData;
        }

        private List<ActiveUserViewModel> GetMostActiveUsers(SqlConnection connection, int limit = 10)
        {
            var activeUsers = new List<ActiveUserViewModel>();

            var query = @"SELECT TOP (@Limit)
                            c.ClientId,
                            c.Prenom,
                            c.Nom,
                            u.Email,
                            c.DerniereConnexion,
                            (SELECT COUNT(*) FROM Commandes WHERE ClientId = c.ClientId) as OrderCount
                         FROM Clients c
                         INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                         WHERE c.DerniereConnexion IS NOT NULL
                         ORDER BY c.DerniereConnexion DESC";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Limit", limit);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activeUsers.Add(new ActiveUserViewModel
                        {
                            ClientId = reader.GetInt32(0),
                            Prenom = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nom = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Email = reader.GetString(3),
                            DerniereConnexion = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                            OrderCount = reader.GetInt32(5)
                        });
                    }
                }
            }

            return activeUsers;
        }

        private List<UserListViewModel> GetUsersList(SqlConnection connection, string searchTerm, string statusFilter, int page, int pageSize, out int totalCount)
        {
            var users = new List<UserListViewModel>();
            var offset = (page - 1) * pageSize;

            // Build WHERE clause
            var whereClause = "WHERE u.TypeUtilisateur = 'Client'";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClause += " AND (u.Email LIKE @SearchTerm OR c.Nom LIKE @SearchTerm OR c.Prenom LIKE @SearchTerm)";
                parameters.Add(new SqlParameter("@SearchTerm", "%" + searchTerm + "%"));
            }

            if (statusFilter == "active")
            {
                whereClause += " AND c.EstActif = 1";
            }
            else if (statusFilter == "inactive")
            {
                whereClause += " AND c.EstActif = 0";
            }

            // Get total count
            var countQuery = $@"SELECT COUNT(*) 
                               FROM Utilisateurs u
                               LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                               {whereClause}";

            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                // Add parameters to count command (create new instances)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countCommand.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                }
                totalCount = (int)countCommand.ExecuteScalar();
            }

            // Get paginated results
            var query = $@"SELECT 
                            c.ClientId,
                            u.UtilisateurId,
                            c.Prenom,
                            c.Nom,
                            u.Email,
                            c.Telephone,
                            (SELECT TOP 1 Ville FROM Adresses a WHERE a.ClientId = c.ClientId ORDER BY a.EstParDefaut DESC, a.AdresseId DESC) as Ville,
                            (SELECT ISNULL(SUM(TotalTTC),0) FROM Commandes WHERE ClientId = c.ClientId) as TotalDepense,
                            c.EstActif,
                            u.DateCreation,
                            c.DerniereConnexion,
                            (SELECT COUNT(*) FROM Commandes WHERE ClientId = c.ClientId) as OrderCount
                         FROM Utilisateurs u
                         LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                         {whereClause}
                         ORDER BY u.DateCreation DESC
                         OFFSET @Offset ROWS
                         FETCH NEXT @PageSize ROWS ONLY";

            using (var command = new SqlCommand(query, connection))
            {
                // Add parameters to main command (create new instances)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    command.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                }
                command.Parameters.AddWithValue("@Offset", offset);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserListViewModel
                        {
                            ClientId = reader.GetInt32(0),
                            UtilisateurId = reader.GetInt32(1),
                            Prenom = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Nom = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Email = reader.GetString(4),
                            Telephone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Ville = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            TotalDepense = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                            EstActif = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                            DateCreation = reader.GetDateTime(9),
                            DerniereConnexion = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                            OrderCount = reader.GetInt32(11)
                        });
                    }
                }
            }

            return users;
        }

        private UserDetailsViewModel GetUserDetails(SqlConnection connection, int clientId)
        {
            var query = @"SELECT 
                            c.ClientId,
                            u.UtilisateurId,
                            c.Prenom,
                            c.Nom,
                            u.Email,
                            c.Telephone,
                            c.DateNaissance,
                            c.EstActif,
                            u.DateCreation,
                            c.DerniereConnexion,
                            (SELECT COUNT(*) FROM Commandes WHERE ClientId = c.ClientId) as OrderCount,
                            (SELECT SUM(TotalTTC) FROM Commandes WHERE ClientId = c.ClientId) as TotalSpent
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
                        return new UserDetailsViewModel
                        {
                            ClientId = reader.GetInt32(0),
                            UtilisateurId = reader.GetInt32(1),
                            Prenom = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Nom = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Email = reader.GetString(4),
                            Telephone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            DateNaissance = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                            EstActif = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                            DateCreation = reader.GetDateTime(8),
                            DerniereConnexion = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9),
                            OrderCount = reader.GetInt32(10),
                            TotalSpent = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11)
                        };
                    }
                }
            }

            return null;
        }

        private DashboardViewModel GetDashboardStats(SqlConnection connection, DashboardViewModel dashboard)
        {
            // Total Sales
            var salesQuery = @"SELECT 
                                ISNULL(SUM(TotalTTC), 0) as TotalSales,
                                ISNULL(SUM(CASE WHEN DateCommande >= DATEADD(DAY, -30, GETDATE()) THEN TotalTTC ELSE 0 END), 0) as LastMonthSales
                              FROM Commandes
                              WHERE Statut != 'Annulée'";
            
            using (var command = new SqlCommand(salesQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dashboard.TotalSales = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                        var lastMonthSales = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                        var previousMonthSales = dashboard.TotalSales - lastMonthSales;
                        dashboard.SalesChangePercent = previousMonthSales > 0 
                            ? Math.Round(((lastMonthSales - previousMonthSales) / previousMonthSales) * 100, 1)
                            : 0;
                    }
                }
            }

            // Orders Today
            var ordersTodayQuery = @"SELECT 
                                        COUNT(*) as OrdersToday,
                                        (SELECT COUNT(*) FROM Commandes 
                                         WHERE CAST(DateCommande AS DATE) = CAST(DATEADD(DAY, -1, GETDATE()) AS DATE)) as YesterdayOrders
                                     FROM Commandes
                                     WHERE CAST(DateCommande AS DATE) = CAST(GETDATE() AS DATE)";
            
            using (var command = new SqlCommand(ordersTodayQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dashboard.OrdersToday = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        var yesterdayOrders = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        dashboard.OrdersTodayChange = dashboard.OrdersToday - yesterdayOrders;
                    }
                }
            }

            // Active Products
            var activeProductsQuery = "SELECT COUNT(*) FROM Produits WHERE EstDisponible = 1";
            using (var command = new SqlCommand(activeProductsQuery, connection))
            {
                dashboard.ActiveProducts = (int)command.ExecuteScalar();
            }

            // Total Users
            var usersQuery = @"SELECT 
                                COUNT(*) as TotalUsers,
                                (SELECT COUNT(*) FROM Utilisateurs 
                                 WHERE TypeUtilisateur = 'Client' 
                                 AND DateCreation >= DATEADD(DAY, -30, GETDATE())) as NewUsers
                              FROM Utilisateurs
                              WHERE TypeUtilisateur = 'Client'";
            
            using (var command = new SqlCommand(usersQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dashboard.TotalUsers = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        dashboard.UsersChange = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    }
                }
            }

            // Total Orders
            var totalOrdersQuery = "SELECT COUNT(*) FROM Commandes";
            using (var command = new SqlCommand(totalOrdersQuery, connection))
            {
                dashboard.TotalOrders = (int)command.ExecuteScalar();
            }

            // Graphs Data - Sales last 7 days
            dashboard.SaleDates = new List<string>();
            dashboard.SaleValues = new List<decimal>();
            dashboard.OrderStatusCounts = new Dictionary<string, int>();

            var salesGraphQuery = @"SELECT 
                                        CAST(DateCommande AS DATE) as SaleDate, 
                                        SUM(TotalTTC) as DailyTotal
                                    FROM Commandes
                                    WHERE DateCommande >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                                    AND Statut != 'Annulée'
                                    GROUP BY CAST(DateCommande AS DATE)
                                    ORDER BY SaleDate";

            using (var command = new SqlCommand(salesGraphQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    var salesDict = new Dictionary<DateTime, decimal>();
                    while (reader.Read())
                    {
                        salesDict.Add(reader.GetDateTime(0), reader.GetDecimal(1));
                    }

                    // Fill last 7 days including empty ones
                    for (int i = 6; i >= 0; i--)
                    {
                        var date = DateTime.Today.AddDays(-i);
                        dashboard.SaleDates.Add(date.ToString("dd/MM"));
                        dashboard.SaleValues.Add(salesDict.ContainsKey(date) ? salesDict[date] : 0);
                    }
                }
            }

            // Graphs Data - Order Status distribution
            var statusQuery = @"SELECT Statut, COUNT(*) 
                                FROM Commandes 
                                GROUP BY Statut";
            
            using (var command = new SqlCommand(statusQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string status = reader.GetString(0);
                        dashboard.OrderStatusCounts[status] = reader.GetInt32(1);
                    }
                }
            }

            return dashboard;
        }

        private List<RecentOrderViewModel> GetRecentOrders(SqlConnection connection, int limit = 4)
        {
            var orders = new List<RecentOrderViewModel>();
            
            var query = @"SELECT TOP (@Limit)
                            c.NumeroCommande,
                            cl.Prenom + ' ' + LEFT(cl.Nom, 1) + '.' as CustomerName,
                            c.TotalTTC,
                            c.Statut,
                            c.DateCommande
                         FROM Commandes c
                         INNER JOIN Clients cl ON c.ClientId = cl.ClientId
                         ORDER BY c.DateCommande DESC";
            
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Limit", limit);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new RecentOrderViewModel
                        {
                            OrderNumber = reader.GetString(0),
                            CustomerName = reader.GetString(1),
                            Amount = reader.GetDecimal(2),
                            Status = reader.GetString(3),
                            OrderDate = reader.GetDateTime(4)
                        });
                    }
                }
            }
            
            return orders;
        }

        private List<LowStockProductViewModel> GetLowStockProducts(SqlConnection connection)
        {
            var products = new List<LowStockProductViewModel>();
            
            var query = @"SELECT 
                            ProduitId,
                            Nom,
                            StockTotal,
                            SeuilAlerte
                         FROM Produits
                         WHERE StockTotal <= SeuilAlerte
                         AND EstDisponible = 1
                         ORDER BY StockTotal ASC";
            
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new LowStockProductViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Stock = reader.GetInt32(2),
                            AlertThreshold = reader.GetInt32(3)
                        });
                    }
                }
            }
            
            return products;
        }

        private List<BestSellingProductViewModel> GetBestSellingProducts(SqlConnection connection, int limit = 5)
        {
            var products = new List<BestSellingProductViewModel>();
            
            var query = @"SELECT TOP (@Limit)
                            p.ProduitId,
                            p.Nom,
                            ISNULL(SUM(ci.Quantite), 0) as SalesCount,
                            p.Prix
                         FROM Produits p
                         LEFT JOIN CommandeItems ci ON p.ProduitId = ci.ProduitId
                         LEFT JOIN Commandes c ON ci.CommandeId = c.CommandeId
                         WHERE (c.Statut != 'Annulée' OR c.Statut IS NULL)
                         GROUP BY p.ProduitId, p.Nom, p.Prix
                         HAVING ISNULL(SUM(ci.Quantite), 0) >= 3
                         ORDER BY SalesCount DESC";
            
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Limit", limit);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new BestSellingProductViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            SalesCount = reader.GetInt32(2),
                            Price = reader.GetDecimal(3)
                        });
                    }
                }
            }
            
            return products;
        }

        private List<CooperativeViewModel> GetCooperatives(SqlConnection connection)
        {
            var cooperatives = new List<CooperativeViewModel>();
            
            var query = @"SELECT 
                            CooperativeId,
                            Nom,
                            Ville,
                            EstActive
                         FROM Cooperatives
                         WHERE EstActive = 1
                         ORDER BY Nom";
            
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cooperatives.Add(new CooperativeViewModel
                        {
                            CooperativeId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            City = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            IsActive = reader.IsDBNull(3) ? false : reader.GetBoolean(3)
                        });
                    }
                }
            }
            
            return cooperatives;
        }
    }
}
