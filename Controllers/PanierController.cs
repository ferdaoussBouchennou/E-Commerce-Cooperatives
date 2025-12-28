using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class PanierController : Controller
    {
        // GET: Panier
        public ActionResult Index()
        {
            ViewBag.Title = "Mon Panier";
            return View();
        }

        [HttpPost]
        public JsonResult GetCartDetails(List<CartItemRequest> items)
        {
            if (items == null || !items.Any())
            {
                return Json(new { success = true, items = new List<object>() });
            }

            try
            {
                using (var db = new ECommerceDbContext())
                {
                    var result = new List<object>();
                    foreach (var item in items)
                    {
                        var produit = db.GetProduitDetails(item.ProduitId);
                        if (produit != null)
                        {
                            var mainImage = produit.ImageUrl 
                                            ?? produit.Images?.FirstOrDefault(img => img.EstPrincipale)?.UrlImage 
                                            ?? produit.Images?.FirstOrDefault()?.UrlImage 
                                            ?? "/Content/images/default-product.jpg";

                            // Normalisation stricte du chemin d'image pour le frontend
                            if (!string.IsNullOrEmpty(mainImage))
                            {
                                mainImage = mainImage.Replace("\\", "/");
                                if (mainImage.StartsWith("~"))
                                {
                                    mainImage = mainImage.Substring(1);
                                }
                                if (!mainImage.StartsWith("/") && !mainImage.StartsWith("http"))
                                {
                                    mainImage = "/" + mainImage;
                                }
                            }

                            var variante = item.VarianteId.HasValue 
                                ? produit.Variantes?.FirstOrDefault(v => v.VarianteId == item.VarianteId.Value)
                                : null;

                            result.Add(new
                            {
                                produitId = produit.ProduitId,
                                nom = produit.Nom,
                                prixUnitaire = Math.Round(produit.Prix * 1.20m, 2) + (variante != null ? Math.Round(variante.PrixSupplementaire * 1.20m, 2) : 0),
                                imageUrl = mainImage,
                                varianteId = item.VarianteId,
                                varianteNom = variante != null ? (variante.Taille ?? variante.Couleur) : null,
                                cooperativeNom = produit.Cooperative?.Nom,
                                quantite = item.Quantite
                            });
                        }
                    }
                    return Json(new { success = true, items = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class CartItemRequest
    {
        public int ProduitId { get; set; }
        public int? VarianteId { get; set; }
        public int Quantite { get; set; }
    }
}
