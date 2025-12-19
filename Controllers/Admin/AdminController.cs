using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class AdminController : Controller
    {
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
        /// Affiche le formulaire d'ajout de produit
        /// </summary>
        public ActionResult AjouterProduit()
        {
            try
            {
                // Charger les catégories et coopératives pour les dropdowns
                ViewBag.Categories = db.Categories.Where(c => c.EstActive).ToList();
                ViewBag.Cooperatives = db.Cooperatives.Where(c => c.EstActive).ToList();

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
        public ActionResult AjouterProduit(Produit produit)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    produit.DateCreation = DateTime.Now;
                    db.ProduitsSet.Add(produit);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Produit ajouté avec succès";
                    return RedirectToAction("Produits");
                }

                // Recharger les listes pour les dropdowns en cas d'erreur
                ViewBag.Categories = db.Categories.Where(c => c.EstActive).ToList();
                ViewBag.Cooperatives = db.Cooperatives.Where(c => c.EstActive).ToList();

                return View(produit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans AjouterProduit POST: {ex.Message}");
                TempData["ErrorMessage"] = "Erreur lors de l'ajout du produit";

                ViewBag.Categories = db.Categories.Where(c => c.EstActive).ToList();
                ViewBag.Cooperatives = db.Cooperatives.Where(c => c.EstActive).ToList();

                return View(produit);
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
        public ActionResult ModifierProduit(Produit produit)
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

                    // Mettre à jour les propriétés
                    produitExistant.Nom = produit.Nom;
                    produitExistant.Description = produit.Description;
                    produitExistant.Prix = produit.Prix;
                    produitExistant.ImageUrl = produit.ImageUrl;
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

                    TempData["SuccessMessage"] = "Produit modifié avec succès";
                    return RedirectToAction("Produits");
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

        /// <summary>
        /// Supprime un produit (AJAX)
        /// </summary>
        [HttpPost]
        public JsonResult SupprimerProduit(int id)
        {
            try
            {
                var produit = db.ProduitsSet.Find(id);

                if (produit == null)
                {
                    return Json(new { success = false, message = "Produit non trouvé" });
                }

                // Vérifier si le produit est dans des commandes
                var commandesAvecProduit = db.CommandeItems.Any(ci => ci.ProduitId == id);

                if (commandesAvecProduit)
                {
                    return Json(new { success = false, message = "Impossible de supprimer : le produit est présent dans des commandes" });
                }

                db.ProduitsSet.Remove(produit);
                db.SaveChanges();

                return Json(new { success = true, message = "Produit supprimé avec succès" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans SupprimerProduit: {ex.Message}");
                return Json(new { success = false, message = "Erreur lors de la suppression" });
            }
        }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}