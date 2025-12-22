using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;
using E_Commerce_Cooperatives.Models.ViewModels;
using System.Data.SqlClient;
using System.Configuration;

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
                        PrixUnitaire = item.PrixUnitaire,
                        TotalLigne = item.TotalLigne
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
                var p = db.ProduitsSet.Find(id);
                if (p == null) return Json(new { success = false, message = "Produit non trouvé" }, JsonRequestBehavior.AllowGet);

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
                    }
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
        public ActionResult AjouterProduit(Produit produit, System.Web.HttpPostedFileBase imageFile)
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

                    if (Request.IsAjaxRequest())
                    {
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
        public ActionResult ModifierProduit(Produit produit, System.Web.HttpPostedFileBase imageFile)
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

                    if (Request.IsAjaxRequest())
                    {
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

                db.ProduitsSet.Remove(p);
                db.SaveChanges();
                return Json(new { success = true });
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
                var zonesPaged = db.GetZonesLivraisonPaged(page, 5);
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
                var categories = db.GetCategoriesWithStats();
                
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

                // Statistiques générales
                var stats = GetUserStatistics(connection);

                // Graphiques d'évolution
                var evolutionData = GetUserEvolutionData(connection);
                ViewBag.EvolutionData = evolutionData;

                // Utilisateurs les plus actifs
                var activeUsers = GetMostActiveUsers(connection);
                ViewBag.ActiveUsers = activeUsers;

                return View(stats);
            }
        }

        // GET: Admin/Utilisateurs
        public ActionResult Utilisateurs(string searchTerm = "", string statusFilter = "all", int page = 1, int pageSize = 20)
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

                    var updateQuery = @"UPDATE Clients 
                                       SET EstActif = @EstActif 
                                       WHERE ClientId = @ClientId";
                    
                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EstActif", isActive);
                        command.Parameters.AddWithValue("@ClientId", userId);
                        command.ExecuteNonQuery();
                    }

                    return Json(new { success = true, message = isActive ? "Compte activé avec succès" : "Compte désactivé avec succès" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur: " + ex.Message });
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
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(param);
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
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
                command.Parameters.Add(new SqlParameter("@Offset", offset));
                command.Parameters.Add(new SqlParameter("@PageSize", pageSize));

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
    }
}
