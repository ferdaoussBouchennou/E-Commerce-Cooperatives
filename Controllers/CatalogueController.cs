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
        public ActionResult Index(int? categorie)
        {
            using (var db = new ECommerceDbContext())
            {
                // Récupérer tous les produits disponibles
                var produits = db.GetProduits();
                
                // Si une catégorie est spécifiée dans l'URL, filtrer
                if (categorie.HasValue)
                {
                    produits = produits.Where(p => p.CategorieId == categorie.Value).ToList();
                }
                
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
                
                // Vérifier si l'utilisateur est authentifié
                ViewBag.IsAuthenticated = Session["UserId"] != null || Session["ClientId"] != null;
            }
            
            return View();
        }
    }
}

