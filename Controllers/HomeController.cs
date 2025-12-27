using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new ECommerceDbContext())
            {
                // Récupérer les catégories
                var categories = db.GetCategories();
                
                // Récupérer les produits vedettes
                var produitsVedettes = db.GetProduits(estEnVedette: true).Take(4).ToList();
                
                // Récupérer les nouveaux produits
                var nouveauxProduits = db.GetProduits(estNouveau: true).Take(4).ToList();
                
                ViewBag.Categories = categories;
                ViewBag.ProduitsVedettes = produitsVedettes;
                ViewBag.NouveauxProduits = nouveauxProduits;
            }
            
            // Vérifier si l'utilisateur est authentifié
            // TODO: Remplacer par votre système d'authentification réel
            // Pour l'instant, on vérifie si une session utilisateur existe
            ViewBag.IsAuthenticated = Session["UserId"] != null || Session["ClientId"] != null;
            
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult TestChatbot()
        {
            return View();
        }
    }
}