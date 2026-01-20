using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class FavorisController : Controller
    {
        // GET: Favoris
        public ActionResult Index()
        {
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Favoris") });
            }

            int clientId = (int)Session["ClientId"];
            using (var db = new ECommerceDbContext())
            {
                var favoris = db.Favoris
                    .Where(f => f.ClientId == clientId)
                    .ToList();

                var produits = new List<Produit>();
                foreach (var f in favoris)
                {
                    var p = db.GetProduitDetails(f.ProduitId);
                    if (p != null)
                    {
                        produits.Add(p);
                    }
                }

                ViewBag.Title = "Mes Favoris";
                return View(produits);
            }
        }

        [HttpPost]
        public JsonResult Toggle(int produitId)
        {
            if (Session["ClientId"] == null)
            {
                return Json(new { success = false, message = "Veuillez vous connecter pour gérer vos favoris.", redirect = true });
            }

            int clientId = (int)Session["ClientId"];
            try
            {
                using (var db = new ECommerceDbContext())
                {
                    bool isFavori = db.IsFavori(clientId, produitId);
                    if (isFavori)
                    {
                        db.RemoveFavori(clientId, produitId);
                        return Json(new { success = true, action = "removed", message = "Produit retiré des favoris." });
                    }
                    else
                    {
                        db.AddFavori(clientId, produitId);
                        return Json(new { success = true, action = "added", message = "Produit ajouté aux favoris." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Une erreur est survenue : " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetStatus(int produitId)
        {
            if (Session["ClientId"] == null)
            {
                return Json(new { isFavori = false }, JsonRequestBehavior.AllowGet);
            }

            int clientId = (int)Session["ClientId"];
            using (var db = new ECommerceDbContext())
            {
                bool isFavori = db.IsFavori(clientId, produitId);
                return Json(new { isFavori = isFavori }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
