using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class CooperativesController : Controller
    {
        public ActionResult Index(string search = null)
        {
            using (var db = new ECommerceDbContext())
            {
                // Récupérer toutes les coopératives actives
                var cooperatives = db.GetCooperatives();
                
                // Filtrer par recherche si fourni
                if (!string.IsNullOrEmpty(search))
                {
                    cooperatives = cooperatives.Where(c => 
                        c.Nom.ToLower().Contains(search.ToLower()) ||
                        (c.Ville != null && c.Ville.ToLower().Contains(search.ToLower()))
                    ).ToList();
                }
                
                // Récupérer les comptes de produits par coopérative
                var productCounts = db.GetProductCountsByCooperative();
                
                // Ajouter le nombre de produits à chaque coopérative
                foreach (var coop in cooperatives)
                {
                    coop.ProductCount = productCounts.ContainsKey(coop.CooperativeId) 
                        ? productCounts[coop.CooperativeId] 
                        : 0;
                }
                
                // Trier les coopératives par nombre de produits décroissant
                cooperatives = cooperatives.OrderByDescending(c => c.ProductCount).ToList();
                
                // Calculer les statistiques
                var totalCooperatives = db.GetCooperatives().Count;
                var totalProducts = db.GetProduits(null, null, null, 1, int.MaxValue, null, null, null, null, null, false, null)
                    .Count(p => p.CooperativeId.HasValue);
                
                // Compter les régions uniques
                var uniqueRegions = db.GetCooperatives()
                    .Where(c => !string.IsNullOrEmpty(c.Ville))
                    .Select(c => c.Ville)
                    .Distinct()
                    .Count();
                
                ViewBag.Cooperatives = cooperatives;
                ViewBag.SearchTerm = search;
                ViewBag.TotalCooperatives = totalCooperatives;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.UniqueRegions = uniqueRegions;
                
                // Vérifier si l'utilisateur est authentifié
                ViewBag.IsAuthenticated = Session["UserId"] != null || Session["ClientId"] != null;
            }
            
            return View();
        }
    }
}

