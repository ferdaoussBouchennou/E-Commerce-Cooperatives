using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace E_Commerce_Cooperatives
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route pour la confirmation de commande avec numéro de commande
            routes.MapRoute(
                name: "OrderConfirmation",
                url: "commande-confirmation/{orderNumber}",
                defaults: new { controller = "Checkout", action = "Confirmation", orderNumber = UrlParameter.Optional }
            );

            // Route pour mes commandes
            routes.MapRoute(
                name: "MesCommandes",
                url: "mes-commandes",
                defaults: new { controller = "Checkout", action = "MesCommandes" }
            );

            // Route pour le suivi de livraison
            routes.MapRoute(
                name: "SuiviLivraison",
                url: "suivi-livraison",
                defaults: new { controller = "Checkout", action = "SuiviLivraison" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
