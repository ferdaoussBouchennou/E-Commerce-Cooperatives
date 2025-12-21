using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class CategoriesController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new ECommerceDbContext())
            {
                // Fetch all active categories
                var categories = db.GetCategories();
                
                // Get total products count
                var totalProducts = db.GetProduitsCount();
                
                // Get product count per category
                var allProducts = db.GetProduits(null, null, null, 1, int.MaxValue);
                var productCountByCategory = allProducts
                    .Where(p => p.CategorieId.HasValue)
                    .GroupBy(p => p.CategorieId.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Fetch top 2 popular products for each category
                var popularProductsByCategory = new Dictionary<int, List<Produit>>();
                foreach (var cat in categories)
                {
                    var popular = db.GetProduits(null, null, cat.CategorieId, 1, 2, null, "popular");
                    popularProductsByCategory[cat.CategorieId] = popular;
                }

                ViewBag.Categories = categories;
                ViewBag.TotalCategories = categories.Count;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.ProductCountByCategory = productCountByCategory;
                ViewBag.PopularProductsByCategory = popularProductsByCategory;
                
                ViewBag.IsAuthenticated = Session["UserId"] != null || Session["ClientId"] != null;

                return View();
            }
        }
    }
}
