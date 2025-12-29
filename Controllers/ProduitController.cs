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
                ViewBag.ClientId = Session["ClientId"];
                
                // Check if user can review (only for delivered products)
                bool canReview = false;
                bool isLoggedIn = Session["ClientId"] != null;
                bool hasDelivered = false;
                bool hasReviewed = false;
                
                if (isLoggedIn)
                {
                    int clientId = (int)Session["ClientId"];
                    hasDelivered = db.HasDeliveredProduct(clientId, id);
                    hasReviewed = db.HasReviewedProduct(clientId, id);
                    canReview = hasDelivered && !hasReviewed;
                }
                
                ViewBag.CanReview = canReview;
                ViewBag.IsLoggedIn = isLoggedIn;
                ViewBag.HasDelivered = hasDelivered;
                ViewBag.HasReviewed = hasReviewed;

                return View();
            }
        }

        [HttpPost]
        public ActionResult AddAvis(int produitId, int note, string commentaire)
        {
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];
            using (var db = new ECommerceDbContext())
            {
                // Verify that the product has been delivered
                if (!db.HasDeliveredProduct(clientId, produitId))
                {
                     TempData["Error"] = "Vous devez avoir reçu ce produit (livré) pour pouvoir laisser un avis.";
                     return RedirectToAction("Details", new { id = produitId });
                }

                if (db.HasReviewedProduct(clientId, produitId))
                {
                     TempData["Error"] = "Vous avez déjà laissé un avis pour ce produit.";
                     return RedirectToAction("Details", new { id = produitId });
                }

                var avis = new AvisProduit
                {
                    ClientId = clientId,
                    ProduitId = produitId,
                    Note = note,
                    Commentaire = commentaire
                };
                db.AddAvis(avis);
                TempData["Success"] = "Votre avis a été ajouté avec succès.";
            }

            return RedirectToAction("Details", new { id = produitId });
        }

        [HttpPost]
        public ActionResult UpdateAvis(int avisId, int produitId, int note, string commentaire)
        {
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];
            using (var db = new ECommerceDbContext())
            {
                var avis = new AvisProduit
                {
                    AvisId = avisId,
                    ClientId = clientId,
                    ProduitId = produitId,
                    Note = note, // Add Note update logic in DbContext if needed, but UpdateAvis handles it
                    Commentaire = commentaire
                };
                db.UpdateAvis(avis);
                TempData["Success"] = "Votre avis a été modifié avec succès.";
            }

            return RedirectToAction("Details", new { id = produitId });
        }

        [HttpPost]
        public ActionResult DeleteAvis(int avisId, int produitId)
        {
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];
            using (var db = new ECommerceDbContext())
            {
                db.DeleteAvis(avisId, clientId);
                TempData["Success"] = "Votre avis a été supprimé.";
            }

            return RedirectToAction("Details", new { id = produitId });
        }
    }
}

