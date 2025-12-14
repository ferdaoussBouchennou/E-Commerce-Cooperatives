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
        public ActionResult Index(int? categorie, string search = null, string sort = "popular", int page = 1, decimal? minPrice = null, decimal? maxPrice = null, string coops = null, bool onlyAvailable = false)
        {
            int pageSize = 9;

            using (var db = new ECommerceDbContext())
            {
                // Parse cooperative IDs
                List<int> cooperativeIds = new List<int>();
                if (!string.IsNullOrEmpty(coops))
                {
                    cooperativeIds = coops.Split(',').Select(int.Parse).ToList();
                }

                // Récupérer les produits pour la page actuelle
                var produits = db.GetProduits(null, null, categorie, page, pageSize, search, sort, minPrice, maxPrice, cooperativeIds, onlyAvailable);
                
                // Récupérer le nombre total de produits pour cette catégorie
                var totalProduits = db.GetProduitsCount(null, null, categorie, search, minPrice, maxPrice, cooperativeIds, onlyAvailable);
                
                // Calculer le nombre de pages
                var totalPages = (int)Math.Ceiling((double)totalProduits / pageSize);
                
                // Récupérer les catégories pour les filtres
                var categories = db.GetCategories();
                
                // Récupérer les coopératives pour les filtres
                var cooperatives = db.GetCooperatives();
                
                // Compter le nombre de produits par catégorie
                var productCountByCategory = produits
                    .Where(p => p.CategorieId.HasValue)
                    .GroupBy(p => p.CategorieId.Value)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                ViewBag.Produits = produits;
                ViewBag.Categories = categories;
                ViewBag.Cooperatives = cooperatives;
                ViewBag.ProductCountByCategory = productCountByCategory;
                ViewBag.SelectedCategorie = categorie;
                ViewBag.SearchTerm = search;
                ViewBag.CurrentSort = sort;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.SelectedCoops = cooperativeIds;
                ViewBag.OnlyAvailable = onlyAvailable;
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

