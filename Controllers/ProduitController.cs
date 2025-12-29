using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class ProduitController : Controller
    {
        public ActionResult Details(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var produit = db.GetProduitDetails(id);
                if (produit == null)
                {
                    return HttpNotFound("Produit non trouvé");
                }
                
                // Récupérer les produits similaires (même catégorie)
                var produitsSimilaires = new List<Produit>();
                if (produit.CategorieId.HasValue)
                {
                    produitsSimilaires = db.GetProduits(
                        null, null, produit.CategorieId, 1, 4, null, null, null, null, null, false, null
                    ).Where(p => p.ProduitId != produit.ProduitId).Take(4).ToList();
                }
                
                ViewBag.Produit = produit;
                ViewBag.ProduitsSimilaires = produitsSimilaires;
                ViewBag.IsAuthenticated = Session["UserId"] != null || Session["ClientId"] != null;
                
                return View();
            }
        }
    }
}

