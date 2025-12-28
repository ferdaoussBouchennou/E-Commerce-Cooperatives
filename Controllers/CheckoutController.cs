using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using E_Commerce_Cooperatives.Models;
using E_Commerce_Cooperatives.Models.ViewModels;

namespace E_Commerce_Cooperatives.Controllers
{
    public class CheckoutController : Controller
    {
        // GET: Checkout
        public ActionResult Index()
        {
            // Vérifier si l'utilisateur est connecté
            if (Session["ClientId"] == null)
            {
                TempData["ReturnUrl"] = "/Checkout";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout" });
            }

            // Récupérer le panier depuis localStorage via JavaScript
            // Pour l'instant, on prépare la vue avec les modes de livraison
            var viewModel = new CheckoutViewModel();
            
            using (var db = new ECommerceDbContext())
            {
                viewModel.ModesLivraison = db.GetModesLivraison().Where(m => m.EstActif).ToList();
                
                // Charger les zones de livraison pour le select de ville
                ViewBag.ZonesLivraison = db.GetZonesLivraison().Where(z => z.EstActif).OrderBy(z => z.ZoneVille).ToList();
                
                // Pré-remplir avec les données du client si disponibles
                if (Session["Prenom"] != null) viewModel.Prenom = Session["Prenom"].ToString();
                if (Session["Nom"] != null) viewModel.Nom = Session["Nom"].ToString();
                if (Session["Email"] != null) viewModel.Email = Session["Email"].ToString();
                if (Session["Telephone"] != null) viewModel.Telephone = Session["Telephone"].ToString();
            }

            return View(viewModel);
        }

        // POST: Checkout/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ProcessOrder(CheckoutViewModel model)
        {
            try
            {
                // Vérifier si l'utilisateur est connecté
                if (Session["ClientId"] == null)
                {
                    return Json(new { success = false, message = "Vous devez être connecté pour passer une commande." });
                }

                int clientId = (int)Session["ClientId"];

                // Récupérer les items du panier depuis la requête
                var cartItemsJson = Request.Form["cartItems"];
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return Json(new { success = false, message = "Votre panier est vide. Veuillez ajouter des produits avant de commander." });
                }

                // Désérialiser les items du panier
                List<CartItemForOrder> cartItems;
                try
                {
                    cartItems = JsonConvert.DeserializeObject<List<CartItemForOrder>>(cartItemsJson);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Erreur lors de la lecture du panier : " + ex.Message });
                }

                if (cartItems == null || !cartItems.Any())
                {
                    return Json(new { success = false, message = "Votre panier est vide après désérialisation." });
                }

                // Valider les champs obligatoires manuellement (pas de ModelState car le ViewModel contient des propriétés non envoyées)
                if (string.IsNullOrWhiteSpace(model.Prenom))
                    return Json(new { success = false, message = "Le prénom est obligatoire." });
                if (string.IsNullOrWhiteSpace(model.Nom))
                    return Json(new { success = false, message = "Le nom est obligatoire." });
                if (string.IsNullOrWhiteSpace(model.Email))
                    return Json(new { success = false, message = "L'email est obligatoire." });
                if (string.IsNullOrWhiteSpace(model.Telephone))
                    return Json(new { success = false, message = "Le téléphone est obligatoire." });
                if (string.IsNullOrWhiteSpace(model.AdresseComplete))
                    return Json(new { success = false, message = "L'adresse complète est obligatoire." });
                if (string.IsNullOrWhiteSpace(model.Ville))
                    return Json(new { success = false, message = "La ville est obligatoire." });
                if (string.IsNullOrWhiteSpace(model.CodePostal))
                    return Json(new { success = false, message = "Le code postal est obligatoire." });
                if (model.ModeLivraisonId <= 0)
                    return Json(new { success = false, message = "Veuillez sélectionner un mode de livraison." });

                using (var db = new ECommerceDbContext())
                {
                    // Créer ou mettre à jour l'adresse
                    int adresseId;
                    try
                    {
                        adresseId = db.CreateOrUpdateAdresse(
                            clientId,
                            model.AdresseComplete,
                            model.Ville,
                            model.CodePostal,
                            "Maroc"
                        );
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Erreur lors de la création de l'adresse : " + ex.Message });
                    }

                    // Créer la commande
                    string numeroCommande;
                    try
                    {
                        numeroCommande = db.CreateCommande(
                            clientId,
                            adresseId,
                            model.ModeLivraisonId,
                            cartItems,
                            model.Notes
                        );
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Erreur lors de la création de la commande : " + ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "") });
                    }

                    // Calculer le total pour l'email avec le calcul dynamique
                    decimal subtotal = cartItems.Sum(i => i.PrixUnitaire * i.Quantite);
                    decimal deliveryFee = db.CalculateDeliveryPrice(model.ModeLivraisonId, model.Ville);
                    decimal total = subtotal + deliveryFee;

                    // Envoyer l'email de confirmation avec facture
                    try
                    {
                        var commande = db.GetCommandeByNumber(numeroCommande);
                        if (commande != null)
                        {
                            EmailHelper.SendOrderConfirmationEmail(commande);
                        }
                        else
                        {
                            // Fallback (ne devrait pas arriver)
                            string clientFullName = $"{model.Prenom} {model.Nom}";
                            var deliveryMode = db.GetModesLivraison().FirstOrDefault(m => m.ModeLivraisonId == model.ModeLivraisonId);
                            string deliveryMethodName = deliveryMode?.Nom ?? "Standard";
                            EmailHelper.SendOrderConfirmationEmail(model.Email, clientFullName, numeroCommande, cartItems, subtotal, deliveryMethodName, deliveryFee, total);
                        }
                    }
                    catch (Exception ex)
                    {
                        // On n'interrompt pas le processus si l'email échoue, mais on pourrait le logger
                        System.Diagnostics.Debug.WriteLine("Échec de l'envoi de l'email : " + ex.Message);
                    }

                    return Json(new { success = true, numeroCommande = numeroCommande });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Une erreur est survenue : " + ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Détails: " + ex.InnerException.Message;
                }
                return Json(new { success = false, message = errorMessage });
            }
        }

        // GET: Checkout/Confirmation/:orderNumber
        public ActionResult Confirmation(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                return RedirectToAction("Index", "Home");
            }

            // Vérifier si l'utilisateur est connecté
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];

            using (var db = new ECommerceDbContext())
            {
                // Récupérer la commande
                var commandes = db.GetCommandesByClient(clientId);
                var commande = commandes.FirstOrDefault(c => c.NumeroCommande == orderNumber);

                if (commande == null)
                {
                    TempData["ErrorMessage"] = "Commande introuvable.";
                    return RedirectToAction("MesCommandes");
                }

                var viewModel = new OrderConfirmationViewModel
                {
                    NumeroCommande = commande.NumeroCommande,
                    DateCommande = commande.DateCommande,
                    TotalTTC = commande.TotalTTC,
                    Statut = commande.Statut,
                    AdresseLivraison = commande.Adresse,
                    ModeLivraison = commande.ModeLivraison,
                    Items = commande.Items
                };

                return View(viewModel);
            }
        }

        // GET: Checkout/MesCommandes
        public ActionResult MesCommandes(int? id, int page = 1)
        {
            // Vérifier si l'utilisateur est connecté
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];
            int pageSize = 4;

            using (var db = new ECommerceDbContext())
            {
                var allCommandes = db.GetCommandesByClient(clientId);
                
                // Filtrer pour ne garder que les commandes validées (Validée, Préparation, Expédiée) - pas Livrée ni Annulée
                // Seules les commandes avec statut "Validée" et suivants sont affichées aux clients
                var activeCommandes = allCommandes
                    .Where(c => c.Statut == "Validée" || c.Statut == "Préparation" || c.Statut == "Expédiée" || c.Statut == "En cours de livraison")
                    .OrderByDescending(c => c.DateCommande)
                    .ToList();

                int totalCommandes = activeCommandes.Count;
                int totalPages = (int)Math.Ceiling((double)totalCommandes / pageSize);

                // Ensure page is within valid range
                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages) page = totalPages;

                var paginatedCommandes = activeCommandes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var viewModel = new MesCommandesViewModel
                {
                    Commandes = paginatedCommandes,
                    SelectedCommandeId = id,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    IsHistory = false
                };

                if (id.HasValue)
                {
                    // Pour le détail, on cherche dans toutes les commandes au cas où on vient d'un lien direct
                    viewModel.CommandeDetail = allCommandes.FirstOrDefault(c => c.CommandeId == id.Value);
                }

                return View("MesCommandes", viewModel);
            }
        }

        // GET: Checkout/HistoriqueCommandes
        public ActionResult HistoriqueCommandes(int? id, int page = 1)
        {
            // Vérifier si l'utilisateur est connecté
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];
            int pageSize = 4;

            using (var db = new ECommerceDbContext())
            {
                var allCommandes = db.GetCommandesByClient(clientId);

                // Filtrer pour ne garder que l'historique (Livrée ou Annulée)
                var historyCommandes = allCommandes
                    .Where(c => c.Statut == "Livrée" || c.Statut == "Annulée")
                    .OrderByDescending(c => c.DateCommande)
                    .ToList();

                int totalCommandes = historyCommandes.Count;
                int totalPages = (int)Math.Ceiling((double)totalCommandes / pageSize);

                // Ensure page is within valid range
                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages) page = totalPages;

                var paginatedCommandes = historyCommandes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var viewModel = new MesCommandesViewModel
                {
                    Commandes = paginatedCommandes,
                    SelectedCommandeId = id,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    IsHistory = true
                };

                if (id.HasValue)
                {
                    viewModel.CommandeDetail = allCommandes.FirstOrDefault(c => c.CommandeId == id.Value);
                }

                return View("MesCommandes", viewModel);
            }
        }

        // GET: Checkout/SuiviLivraison
        public ActionResult SuiviLivraison()
        {
            return View();
        }

        // GET: Checkout/SearchTracking
        [HttpGet]
        public JsonResult SearchTracking(string trackingNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(trackingNumber))
                {
                    return Json(new { success = false, message = "Veuillez entrer un numéro de commande ou de suivi" }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new ECommerceDbContext())
                {
                    Commande commande = null;
                    
                    // Search by order number first using GetCommandes with search term
                    var allCommandes = db.GetCommandes(searchTerm: trackingNumber);
                    commande = allCommandes.FirstOrDefault(c => 
                        c.NumeroCommande.Equals(trackingNumber, StringComparison.OrdinalIgnoreCase) ||
                        c.NumeroCommande.Contains(trackingNumber));

                    // If not found, try to find by searching all orders and checking tracking numbers
                    if (commande == null)
                    {
                        var allOrders = db.GetCommandes();
                        foreach (var order in allOrders)
                        {
                            if (order.SuiviLivraison != null && order.SuiviLivraison.Any(s => 
                                s.NumeroSuivi != null && s.NumeroSuivi.Contains(trackingNumber)))
                            {
                                commande = order;
                                break;
                            }
                        }
                    }

                    if (commande == null)
                    {
                        return Json(new { success = false, message = "Aucune commande trouvée avec ce numéro. Vérifiez le numéro et réessayez." }, JsonRequestBehavior.AllowGet);
                    }

                    // Get tracking events (real ones from DB)
                    var suiviEvents = commande.SuiviLivraison?.OrderByDescending(s => s.DateStatut).ToList() ?? new List<LivraisonSuivi>();

                    // Calculate estimated delivery date (3-5 days from order date)
                    var estimatedDelivery = commande.DateCommande.AddDays(5);

                    var eventsList = new List<object>();
                    foreach (var s in suiviEvents)
                    {
                        // Convertir "Validée" en "Confirmée" pour l'affichage client
                        var displayStatus = s.Statut == "Validée" ? "Confirmée" : s.Statut;
                        eventsList.Add(new
                        {
                            date = s.DateStatut.ToString("yyyy-MM-dd"),
                            heure = s.DateStatut.ToString("HH:mm"),
                            status = displayStatus,
                            lieu = GetStatusLocation(s.Statut),
                            description = s.Description ?? GetStatusDescription(s.Statut)
                        });
                    }

                    // Convertir "Validée" en "Confirmée" pour l'affichage client
                    var displayStatut = commande.Statut == "Validée" ? "Confirmée" : commande.Statut;
                    
                    var result = new
                    {
                        numeroCommande = commande.NumeroCommande,
                        statut = displayStatut,
                        dateCommande = commande.DateCommande.ToString("yyyy-MM-dd"),
                        dateLivraisonEstimee = estimatedDelivery.ToString("yyyy-MM-dd"),
                        adresseLivraison = commande.Adresse != null 
                            ? $"{commande.Adresse.AdresseComplete}, {commande.Adresse.CodePostal} {commande.Adresse.Ville}"
                            : "Adresse non disponible",
                        transporteur = "Amana Express",
                        numeroSuivi = suiviEvents.FirstOrDefault()?.NumeroSuivi ?? $"AE{commande.NumeroCommande.Replace("CMD-", "")}MA",
                        events = eventsList
                    };

                    return Json(new { success = true, result = result }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Une erreur est survenue : " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private string GetStatusLocation(string statut)
        {
            switch (statut.ToLower())
            {
                case "validée":
                case "validee":
                case "confirmée":
                case "confirme":
                    return "En ligne";
                case "préparation":
                case "preparation":
                    return "Coopérative Atlas";
                case "expédiée":
                case "expedie":
                    return "Entrepôt Marrakech";
                case "en transit":
                case "en_transit":
                    return "Centre de tri Casablanca";
                case "en livraison":
                case "en_livraison":
                    return "Centre de distribution local";
                case "livrée":
                case "livre":
                    return "Adresse de livraison";
                default:
                    return "En traitement";
            }
        }

        private string GetStatusDescription(string status)
        {
            // Normaliser le statut pour gérer "Validée" et "Confirmée"
            var normalizedStatus = status == "Confirmée" ? "Validée" : status;
            
            switch (normalizedStatus)
            {
                case "En attente": return "Votre commande a été reçue et est en attente de confirmation.";
                case "Validée": return "Votre commande a été confirmée et est en cours de traitement.";
                case "Préparation": return "Votre commande est en cours de préparation dans notre entrepôt.";
                case "Expédiée": return "Votre commande a été expédiée et est en route.";
                case "Livrée": return "Votre commande a été livrée avec succès.";
                case "Annulée": return "Votre commande a été annulée.";
                default: return "Statut : " + status;
            }
        }

        // GET: Checkout/DownloadInvoice/5
        public ActionResult DownloadInvoice(int id)
        {
            if (Session["ClientId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int clientId = (int)Session["ClientId"];

            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);

                if (commande == null || commande.ClientId != clientId)
                {
                    return HttpNotFound();
                }

                try
                {
                    byte[] pdfBytes = E_Commerce_Cooperatives.Helpers.InvoiceHelper.GenerateInvoice(commande);
                    return File(pdfBytes, "application/pdf", $"Facture_{commande.NumeroCommande}.pdf");
                }
                catch (Exception ex)
                {
                    // Log error
                    return new HttpStatusCodeResult(500, "Erreur lors de la génération de la facture: " + ex.Message);
                }
            }
        }

        // GET: Checkout/CalculateDeliveryPrice
        [HttpGet]
        public JsonResult CalculateDeliveryPrice(int modeLivraisonId, string ville)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ville))
                {
                    return Json(new { success = false, message = "La ville est requise" }, JsonRequestBehavior.AllowGet);
                }

                if (modeLivraisonId <= 0)
                {
                    return Json(new { success = false, message = "Le mode de livraison est requis" }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new ECommerceDbContext())
                {
                    decimal prixLivraison = db.CalculateDeliveryPrice(modeLivraisonId, ville);
                    var mode = db.GetModeLivraison(modeLivraisonId);
                    var zone = db.GetZoneLivraisonByVille(ville);
                    var delay = db.GetDeliveryDelay(modeLivraisonId, ville);
                    
                    return Json(new 
                    { 
                        success = true, 
                        prixLivraison = prixLivraison,
                        prixBase = mode?.Tarif ?? 0,
                        supplement = zone?.Supplement ?? 0,
                        ville = ville,
                        zoneVille = zone?.ZoneVille ?? ville,
                        delaiMin = ((dynamic)delay).DelaiMin,
                        delaiMax = ((dynamic)delay).DelaiMax,
                        delaiText = ((dynamic)delay).DelaiText
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur lors du calcul: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

