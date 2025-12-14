using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class CatalogueController : Controller
    {
        public ActionResult Index(int? categorie, string search = null, string sort = "popular", int page = 1, decimal? minPrice = null, decimal? maxPrice = null, string coops = null, bool onlyAvailable = false, int? minRating = null)
        {
            int pageSize = 9;

            using (var db = new ECommerceDbContext())
            {
                // Parse cooperative IDs
                List<int> cooperativeIds = new List<int>();
                if (!string.IsNullOrEmpty(coops))
                {
                    cooperativeIds = coops.Split(',')
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Select(int.Parse)
                        .ToList();
                }

                // Get dynamic max and min price from DB
                var dbMaxPrice = db.GetMaxPrice();
                var dbMinPrice = db.GetMinPrice();

                // Si minPrice/maxPrice ne sont pas spécifiés, utiliser les valeurs de la BD
                var effectiveMinPrice = minPrice ?? dbMinPrice;
                var effectiveMaxPrice = maxPrice ?? dbMaxPrice;

                // Récupérer les produits pour la page actuelle
                var produits = db.GetProduits(
                    null, 
                    null, 
                    categorie, 
                    page, 
                    pageSize, 
                    search, 
                    sort, 
                    effectiveMinPrice, 
                    effectiveMaxPrice, 
                    cooperativeIds, 
                    onlyAvailable, 
                    minRating
                );
                
                // Récupérer le nombre total de produits
                var totalProduits = db.GetProduitsCount(
                    null, 
                    null, 
                    categorie, 
                    search, 
                    effectiveMinPrice, 
                    effectiveMaxPrice, 
                    cooperativeIds, 
                    onlyAvailable, 
                    minRating
                );
                
                // Calculer le nombre de pages
                var totalPages = (int)Math.Ceiling((double)totalProduits / pageSize);
                
                // Récupérer les catégories pour les filtres
                var categories = db.GetCategories();
                
                // Récupérer les coopératives pour les filtres
                var cooperatives = db.GetCooperatives();
                
                // Compter le nombre de produits par catégorie (pour affichage dans filtres)
                var allProductsForCounts = db.GetProduits(null, null, null, 1, int.MaxValue, null, null, null, null, null, false, null);
                var productCountByCategory = allProductsForCounts
                    .Where(p => p.CategorieId.HasValue)
                    .GroupBy(p => p.CategorieId.Value)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                // Passer les données à la vue
                ViewBag.Produits = produits;
                ViewBag.Categories = categories;
                ViewBag.Cooperatives = cooperatives;
                ViewBag.ProductCountByCategory = productCountByCategory;
                ViewBag.SelectedCategorie = categorie;
                ViewBag.SearchTerm = search;
                ViewBag.CurrentSort = sort;
                
                // Prix actuellement sélectionnés (ou valeurs par défaut)
                ViewBag.MinPrice = effectiveMinPrice;
                ViewBag.MaxPrice = effectiveMaxPrice;
                
                // Limites globales pour les sliders
                ViewBag.GlobalMaxPrice = dbMaxPrice;
                ViewBag.GlobalMinPrice = dbMinPrice;
                
                ViewBag.SelectedCoops = cooperativeIds;
                ViewBag.OnlyAvailable = onlyAvailable;
                ViewBag.MinRating = minRating;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                
                // Vérifier si l'utilisateur est authentifié
                ViewBag.IsAuthenticated = Session["UserId"] != null || Session["ClientId"] != null;
            }
            
            return View();
        }

        [HttpGet]
        public JsonResult Suggestions(string term)
        {
            using (var db = new ECommerceDbContext())
            {
                var suggestions = db.GetSearchSuggestions(term);
                return Json(suggestions, JsonRequestBehavior.AllowGet);
            }
        }
    }
}