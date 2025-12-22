using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;
using E_Commerce_Cooperatives.Models.ViewModels;
using System.IO;
// Note: iTextSharp requires NuGet package installation: Install-Package iTextSharp
// Run this command in Package Manager Console: Install-Package iTextSharp
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;

namespace E_Commerce_Cooperatives.Controllers.Admin
{
    public class AdminController : Controller
    {
        private string connectionString;

        public AdminController()
        {
            var connection = ConfigurationManager.ConnectionStrings["ECommerceConnection"];
            if (connection != null)
            {
                connectionString = connection.ConnectionString;
            }
            else
            {
                connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ecommerce;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            }
        }

        // GET: Admin
        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // GET: Admin/Commandes
        public ActionResult Commandes(string searchTerm = null, string statutFilter = "all", 
            DateTime? dateFrom = null, DateTime? dateTo = null, 
            decimal? montantMin = null, decimal? montantMax = null)
        {
            using (var db = new ECommerceDbContext())
            {
                var commandes = db.GetCommandes(searchTerm, statutFilter, dateFrom, dateTo, montantMin, montantMax);
                var stats = db.GetCommandeStats();

                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatutFilter = statutFilter;
                ViewBag.DateFrom = dateFrom;
                ViewBag.DateTo = dateTo;
                ViewBag.MontantMin = montantMin;
                ViewBag.MontantMax = montantMax;
                ViewBag.Stats = stats;

                return View(commandes);
            }
        }

        // GET: Admin/Commandes/Details/5
        public ActionResult Details(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);
                if (commande == null)
                {
                    return HttpNotFound();
                }
                
                // Create a simplified object for JSON serialization
                var result = new
                {
                    CommandeId = commande.CommandeId,
                    NumeroCommande = commande.NumeroCommande,
                    DateCommande = commande.DateCommande,
                    Statut = commande.Statut,
                    TotalHT = commande.TotalHT,
                    MontantTVA = commande.MontantTVA,
                    FraisLivraison = commande.FraisLivraison,
                    TotalTTC = commande.TotalTTC,
                    Client = new
                    {
                        Nom = commande.Client?.Nom,
                        Prenom = commande.Client?.Prenom,
                        Email = commande.Client?.Email
                    },
                    Adresse = commande.Adresse != null ? new
                    {
                        AdresseComplete = commande.Adresse.AdresseComplete,
                        Ville = commande.Adresse.Ville,
                        CodePostal = commande.Adresse.CodePostal
                    } : null,
                    ModeLivraison = commande.ModeLivraison != null ? new
                    {
                        Nom = commande.ModeLivraison.Nom
                    } : null,
                    Items = commande.Items?.Select(item => new
                    {
                        Produit = new
                        {
                            Nom = item.Produit?.Nom
                        },
                        Quantite = item.Quantite,
                        PrixUnitaire = item.PrixUnitaire,
                        TotalLigne = item.TotalLigne
                    }).ToList()
                };
                
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin/Commandes/UpdateStatus
        [HttpPost]
        public ActionResult UpdateStatus(int commandeId, string nouveauStatut)
        {
            try
            {
                // Empêcher l'annulation via cette méthode - utiliser Cancel() à la place
                if (nouveauStatut == "Annulée")
                {
                    return Json(new { success = false, message = "Pour annuler une commande, veuillez utiliser le bouton 'Annuler la commande' et fournir une raison d'annulation." });
                }

                // Validation des paramètres
                if (commandeId <= 0)
                {
                    return Json(new { success = false, message = "ID de commande invalide" });
                }

                if (string.IsNullOrWhiteSpace(nouveauStatut))
                {
                    return Json(new { success = false, message = "Le statut est obligatoire" });
                }

                // Vérifier que le statut est valide (sauf Annulée)
                var statutsValides = new[] { "Validée", "Préparation", "Expédiée", "Livrée" };
                if (!statutsValides.Contains(nouveauStatut))
                {
                    return Json(new { success = false, message = "Statut invalide" });
                }

                using (var db = new ECommerceDbContext())
                {
                    // Vérifier que la commande existe
                    var commande = db.GetCommandeDetails(commandeId);
                    if (commande == null)
                    {
                        return Json(new { success = false, message = "Commande introuvable" });
                    }

                    // Empêcher de modifier le statut d'une commande annulée
                    if (commande.Statut == "Annulée")
                    {
                        return Json(new { success = false, message = "Impossible de modifier le statut d'une commande annulée" });
                    }

                    var success = db.UpdateCommandeStatut(commandeId, nouveauStatut);
                    if (success)
                    {
                        return Json(new { success = true, message = "Statut mis à jour avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la mise à jour" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // POST: Admin/Commandes/Cancel
        [HttpPost]
        public ActionResult Cancel(int commandeId, string raisonAnnulation)
        {
            try
            {
                // Validation des paramètres
                if (commandeId <= 0)
                {
                    return Json(new { success = false, message = "ID de commande invalide" });
                }

                if (string.IsNullOrWhiteSpace(raisonAnnulation))
                {
                    return Json(new { success = false, message = "La raison d'annulation est obligatoire" });
                }

                if (raisonAnnulation.Trim().Length < 10)
                {
                    return Json(new { success = false, message = "La raison d'annulation doit contenir au moins 10 caractères" });
                }

                using (var db = new ECommerceDbContext())
                {
                    // Vérifier que la commande existe
                    var commande = db.GetCommandeDetails(commandeId);
                    if (commande == null)
                    {
                        return Json(new { success = false, message = "Commande introuvable" });
                    }

                    // Vérifier que la commande n'est pas déjà annulée
                    if (commande.Statut == "Annulée")
                    {
                        return Json(new { success = false, message = "Cette commande est déjà annulée" });
                    }

                    var success = db.AnnulerCommande(commandeId, raisonAnnulation.Trim());
                    if (success)
                    {
                        return Json(new { success = true, message = "Commande annulée avec succès" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Erreur lors de la mise à jour du statut. Vérifiez que le statut 'Annulée' est autorisé dans la base de données." });
                    }
                }
            }
            catch (Exception ex)
            {
                // Logger l'erreur (à implémenter avec un système de logging)
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // GET: Admin/Commandes/PrintInvoice/5
        public ActionResult PrintInvoice(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);
                if (commande == null)
                {
                    return HttpNotFound();
                }

                return GenerateInvoicePDF(commande);
            }
        }

        // GET: Admin/Commandes/PrintDeliverySlip/5
        public ActionResult PrintDeliverySlip(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var commande = db.GetCommandeDetails(id);
                if (commande == null)
                {
                    return HttpNotFound();
                }

                return GenerateDeliverySlipPDF(commande);
            }
        }

        private ActionResult GenerateInvoicePDF(Commande commande)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 60, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, stream);
                document.Open();

                // Définir les couleurs - Palette du projet CoopShop
                BaseColor primaryColor = new BaseColor(48, 92, 125); // Azul Profundo (#305C7D) - Bleu profond
                BaseColor secondaryColor = new BaseColor(125, 132, 83); // Verde Oliva (#7D8453) - Vert olive
                BaseColor accentColor = new BaseColor(192, 108, 80); // Terracotta Cálido (#C06C50) - Terracotta chaud
                BaseColor headerBgColor = new BaseColor(48, 92, 125); // Azul Profundo pour en-tête
                BaseColor headerTextColor = BaseColor.WHITE;
                BaseColor backgroundWarm = new BaseColor(232, 220, 194); // Arena Suave (#E8DCC2) - Sable doux
                BaseColor backgroundLight = new BaseColor(245, 243, 239); // Blanco Roto (#F5F3EF) - Blanc cassé
                BaseColor darkGray = new BaseColor(97, 97, 97);
                BaseColor borderColor = new BaseColor(224, 224, 224);
                BaseColor textMuted = new BaseColor(90, 108, 125); // Couleur texte secondaire

                // En-tête avec couleur
                PdfPTable headerTable = new PdfPTable(1);
                headerTable.WidthPercentage = 100;
                PdfPCell headerCell = new PdfPCell(new Phrase("FACTURE", FontFactory.GetFont(FontFactory.TIMES_BOLD, 24, headerTextColor)));
                headerCell.BackgroundColor = headerBgColor;
                headerCell.Padding = 15;
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell.Border = Rectangle.NO_BORDER;
                headerTable.AddCell(headerCell);
                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                // Informations de la commande dans un tableau stylisé
                PdfPTable infoTable = new PdfPTable(2);
                infoTable.WidthPercentage = 100;
                infoTable.SetWidths(new float[] { 1, 1 });

                Font normalFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 10);
                Font boldFont = FontFactory.GetFont(FontFactory.TIMES_BOLD, 10);
                Font labelFont = FontFactory.GetFont(FontFactory.TIMES_BOLD, 9, darkGray);

                // Colonne gauche - Informations commande
                PdfPCell infoCell1 = new PdfPCell();
                infoCell1.Border = Rectangle.NO_BORDER;
                infoCell1.Padding = 10;
                Paragraph info1 = new Paragraph();
                info1.Add(new Chunk("Numéro de commande\n", labelFont));
                info1.Add(new Chunk(commande.NumeroCommande + "\n\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 11, primaryColor)));
                info1.Add(new Chunk("Date\n", labelFont));
                info1.Add(new Chunk(commande.DateCommande.ToString("dd/MM/yyyy") + "\n\n", normalFont));
                info1.Add(new Chunk("Statut\n", labelFont));
                info1.Add(new Chunk(commande.Statut ?? "Non défini", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10, secondaryColor)));
                infoCell1.AddElement(info1);
                infoTable.AddCell(infoCell1);

                // Colonne droite - Informations client
                PdfPCell infoCell2 = new PdfPCell();
                infoCell2.Border = Rectangle.NO_BORDER;
                infoCell2.Padding = 10;
                Paragraph info2 = new Paragraph();
                info2.Add(new Chunk("CLIENT\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 12, primaryColor)));
                if (commande.Client != null)
                {
                    info2.Add(new Chunk(commande.Client.NomComplet + "\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                    info2.Add(new Chunk(commande.Client.Email ?? "N/A" + "\n", normalFont));
                }
                else
                {
                    info2.Add(new Chunk("Client non disponible\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                }
                if (commande.Adresse != null)
                {
                    info2.Add(new Chunk("\n" + commande.Adresse.AdresseComplete + "\n", normalFont));
                    info2.Add(new Chunk(commande.Adresse.Ville + ", " + commande.Adresse.CodePostal, normalFont));
                }
                infoCell2.AddElement(info2);
                infoTable.AddCell(infoCell2);

                document.Add(infoTable);
                document.Add(new Paragraph("\n"));

                // Table des produits avec style amélioré
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3, 1, 1.5f, 1.5f });
                table.SpacingBefore = 10f;

                // En-têtes de tableau avec couleur
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, headerTextColor);
                string[] headers = { "Produit", "Qté", "Prix unit.", "Total" };
                foreach (string header in headers)
                {
                    PdfPCell tableHeaderCell = new PdfPCell(new Phrase(header, headerFont));
                    tableHeaderCell.BackgroundColor = headerBgColor;
                    tableHeaderCell.Padding = 8;
                    tableHeaderCell.HorizontalAlignment = header == "Produit" ? Element.ALIGN_LEFT : 
                                                      header == "Qté" ? Element.ALIGN_CENTER : Element.ALIGN_RIGHT;
                    tableHeaderCell.BorderColor = borderColor;
                    table.AddCell(tableHeaderCell);
                }

                // Lignes de produits avec alternance de couleurs
                bool alternate = false;
                if (commande.Items != null && commande.Items.Any())
                {
                    foreach (var item in commande.Items)
                    {
                        if (item?.Produit == null) continue;
                        
                        BaseColor rowColor = alternate ? backgroundLight : BaseColor.WHITE;
                        alternate = !alternate;

                        PdfPCell cell1 = new PdfPCell(new Phrase(item.Produit.Nom ?? "Produit inconnu", normalFont));
                    cell1.BackgroundColor = rowColor;
                    cell1.Padding = 8;
                    cell1.BorderColor = borderColor;
                    table.AddCell(cell1);

                    PdfPCell cell2 = new PdfPCell(new Phrase(item.Quantite.ToString(), normalFont));
                    cell2.BackgroundColor = rowColor;
                    cell2.Padding = 8;
                    cell2.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell2.BorderColor = borderColor;
                    table.AddCell(cell2);

                    PdfPCell cell3 = new PdfPCell(new Phrase(item.PrixUnitaire.ToString("0.00") + " MAD", normalFont));
                    cell3.BackgroundColor = rowColor;
                    cell3.Padding = 8;
                    cell3.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell3.BorderColor = borderColor;
                    table.AddCell(cell3);

                        PdfPCell cell4 = new PdfPCell(new Phrase(item.TotalLigne.ToString("0.00") + " MAD", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                        cell4.BackgroundColor = rowColor;
                        cell4.Padding = 8;
                        cell4.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell4.BorderColor = borderColor;
                        table.AddCell(cell4);
                    }
                }
                else
                {
                    // Aucun produit
                    PdfPCell emptyCell = new PdfPCell(new Phrase("Aucun produit", normalFont));
                    emptyCell.Colspan = 4;
                    emptyCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    emptyCell.Padding = 15;
                    table.AddCell(emptyCell);
                }

                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Totaux dans un tableau stylisé
                PdfPTable totalsTable = new PdfPTable(2);
                totalsTable.WidthPercentage = 50;
                totalsTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalsTable.SetWidths(new float[] { 2, 1.5f });

                // Ligne sous-total HT
                AddTotalRow(totalsTable, "Sous-total HT:", commande.TotalHT.ToString("0.00") + " MAD", normalFont, boldFont, borderColor);
                
                // Ligne TVA
                AddTotalRow(totalsTable, "TVA:", commande.MontantTVA.ToString("0.00") + " MAD", normalFont, normalFont, borderColor);
                
                // Ligne frais de livraison
                AddTotalRow(totalsTable, "Frais de livraison:", commande.FraisLivraison.ToString("0.00") + " MAD", normalFont, normalFont, borderColor);
                
                // Ligne séparatrice
                PdfPCell separator = new PdfPCell(new Phrase(""));
                separator.Border = Rectangle.TOP_BORDER;
                separator.BorderColor = textMuted;
                separator.Colspan = 2;
                separator.Padding = 5;
                separator.FixedHeight = 1f;
                totalsTable.AddCell(separator);
                
                // Ligne TOTAL TTC avec couleur gris bleuté
                PdfPCell totalLabelCell = new PdfPCell(new Phrase("TOTAL TTC", FontFactory.GetFont(FontFactory.TIMES_BOLD, 12, BaseColor.WHITE)));
                totalLabelCell.BackgroundColor = textMuted; // Gris bleuté (#5A6C7D)
                totalLabelCell.Padding = 10;
                totalLabelCell.BorderColor = borderColor;
                totalsTable.AddCell(totalLabelCell);

                PdfPCell totalValueCell = new PdfPCell(new Phrase(commande.TotalTTC.ToString("0.00") + " MAD", FontFactory.GetFont(FontFactory.TIMES_BOLD, 14, BaseColor.WHITE)));
                totalValueCell.BackgroundColor = textMuted; // Gris bleuté (#5A6C7D)
                totalValueCell.Padding = 10;
                totalValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalValueCell.BorderColor = borderColor;
                totalsTable.AddCell(totalValueCell);

                document.Add(totalsTable);

                // Pied de page
                document.Add(new Paragraph("\n\n"));
                Paragraph footer = new Paragraph("Merci pour votre confiance !", FontFactory.GetFont(FontFactory.TIMES_ITALIC, 9, textMuted));
                footer.Alignment = Element.ALIGN_CENTER;
                document.Add(footer);

                document.Close();

                byte[] byteArray = stream.ToArray();
                return File(byteArray, "application/pdf", "Facture_" + commande.NumeroCommande + ".pdf");
            }
        }

        private void AddTotalRow(PdfPTable table, string label, string value, Font normalFont, Font valueFont, BaseColor borderColor)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, normalFont));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 5;
            table.AddCell(labelCell);

            PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.Padding = 5;
            valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            table.AddCell(valueCell);
        }

        private ActionResult GenerateDeliverySlipPDF(Commande commande)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 60, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, stream);
                document.Open();

                // Définir les couleurs - Palette du projet CoopShop
                BaseColor primaryColor = new BaseColor(48, 92, 125); // Azul Profundo (#305C7D) - Bleu profond
                BaseColor secondaryColor = new BaseColor(125, 132, 83); // Verde Oliva (#7D8453) - Vert olive
                BaseColor accentColor = new BaseColor(192, 108, 80); // Terracotta Cálido (#C06C50) - Terracotta chaud
                BaseColor headerBgColor = new BaseColor(48, 92, 125); // Azul Profundo pour en-tête bordereau
                BaseColor headerTextColor = BaseColor.WHITE;
                BaseColor backgroundWarm = new BaseColor(232, 220, 194); // Arena Suave (#E8DCC2) - Sable doux
                BaseColor backgroundLight = new BaseColor(245, 243, 239); // Blanco Roto (#F5F3EF) - Blanc cassé
                BaseColor darkGray = new BaseColor(97, 97, 97);
                BaseColor borderColor = new BaseColor(224, 224, 224);
                BaseColor infoBgColor = new BaseColor(245, 243, 239); // Blanco Roto pour fonds d'info
                BaseColor textMuted = new BaseColor(90, 108, 125); // Couleur texte secondaire (#5A6C7D)

                // En-tête avec couleur
                PdfPTable headerTable = new PdfPTable(1);
                headerTable.WidthPercentage = 100;
                PdfPCell headerCell = new PdfPCell(new Phrase("BORDEREAU DE LIVRAISON", FontFactory.GetFont(FontFactory.TIMES_BOLD, 24, headerTextColor)));
                headerCell.BackgroundColor = headerBgColor;
                headerCell.Padding = 15;
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell.Border = Rectangle.NO_BORDER;
                headerTable.AddCell(headerCell);
                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                Font normalFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 10);
                Font boldFont = FontFactory.GetFont(FontFactory.TIMES_BOLD, 10);
                Font labelFont = FontFactory.GetFont(FontFactory.TIMES_BOLD, 9, textMuted);

                // Informations de la commande dans un tableau stylisé
                PdfPTable infoTable = new PdfPTable(2);
                infoTable.WidthPercentage = 100;
                infoTable.SetWidths(new float[] { 1, 1 });

                PdfPCell infoCell1 = new PdfPCell();
                infoCell1.BackgroundColor = BaseColor.WHITE; // Blanc par défaut
                infoCell1.BorderColor = borderColor;
                infoCell1.Padding = 12;
                Paragraph info1 = new Paragraph();
                info1.Add(new Chunk("INFORMATIONS COMMANDE\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 11, primaryColor)));
                info1.Add(new Chunk("\nNuméro: ", labelFont));
                info1.Add(new Chunk(commande.NumeroCommande + "\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                info1.Add(new Chunk("Date: ", labelFont));
                info1.Add(new Chunk(commande.DateCommande.ToString("dd/MM/yyyy"), normalFont));
                infoCell1.AddElement(info1);
                infoTable.AddCell(infoCell1);

                // Mode de livraison
                PdfPCell infoCell2 = new PdfPCell();
                infoCell2.BackgroundColor = BaseColor.WHITE; // Blanc par défaut
                infoCell2.BorderColor = borderColor;
                infoCell2.Padding = 12;
                Paragraph info2 = new Paragraph();
                if (commande.ModeLivraison != null)
                {
                    info2.Add(new Chunk("MODE DE LIVRAISON\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 11, primaryColor)));
                    info2.Add(new Chunk("\n" + commande.ModeLivraison.Nom + "\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                    if (!string.IsNullOrEmpty(commande.ModeLivraison.Description))
                    {
                        info2.Add(new Chunk(commande.ModeLivraison.Description, normalFont));
                    }
                }
                else
                {
                    info2.Add(new Chunk("MODE DE LIVRAISON\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 11, primaryColor)));
                    info2.Add(new Chunk("\nStandard", normalFont));
                }
                infoCell2.AddElement(info2);
                infoTable.AddCell(infoCell2);

                document.Add(infoTable);
                document.Add(new Paragraph("\n"));

                // Adresse de livraison dans un encadré
                if (commande.Adresse != null)
                {
                    PdfPTable addressTable = new PdfPTable(1);
                    addressTable.WidthPercentage = 100;
                    PdfPCell addressCell = new PdfPCell();
                    addressCell.BackgroundColor = backgroundLight; // Blanco Roto
                    addressCell.BorderColor = primaryColor;
                    addressCell.BorderWidth = 2f;
                    addressCell.Padding = 12;
                    Paragraph address = new Paragraph();
                    address.Add(new Chunk("ADRESSE DE LIVRAISON\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 12, primaryColor)));
                    address.Add(new Chunk("\n" + commande.Adresse.AdresseComplete + "\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                    address.Add(new Chunk(commande.Adresse.Ville + ", " + commande.Adresse.CodePostal + "\n", normalFont));
                    address.Add(new Chunk(commande.Adresse.Pays, normalFont));
                    addressCell.AddElement(address);
                    addressTable.AddCell(addressCell);
                    document.Add(addressTable);
                    document.Add(new Paragraph("\n"));
                }

                // Liste des produits avec style amélioré
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3, 1, 1.5f });
                table.SpacingBefore = 10f;

                // En-têtes de tableau avec couleur
                Font headerFont = FontFactory.GetFont(FontFactory.TIMES_BOLD, 10, headerTextColor);
                string[] headers = { "Produit", "Qté", "Vérifié" };
                foreach (string header in headers)
                {
                    PdfPCell tableHeaderCell = new PdfPCell(new Phrase(header, headerFont));
                    tableHeaderCell.BackgroundColor = headerBgColor;
                    tableHeaderCell.Padding = 8;
                    tableHeaderCell.HorizontalAlignment = header == "Produit" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                    tableHeaderCell.BorderColor = borderColor;
                    table.AddCell(tableHeaderCell);
                }

                // Lignes de produits avec alternance de couleurs
                bool alternate = false;
                if (commande.Items != null && commande.Items.Any())
                {
                    foreach (var item in commande.Items)
                    {
                        if (item?.Produit == null) continue;
                        
                        BaseColor rowColor = alternate ? backgroundLight : BaseColor.WHITE;
                        alternate = !alternate;

                        PdfPCell cell1 = new PdfPCell(new Phrase(item.Produit.Nom ?? "Produit inconnu", normalFont));
                    cell1.BackgroundColor = rowColor;
                    cell1.Padding = 8;
                    cell1.BorderColor = borderColor;
                    table.AddCell(cell1);

                    PdfPCell cell2 = new PdfPCell(new Phrase(item.Quantite.ToString(), FontFactory.GetFont(FontFactory.TIMES_BOLD, 10)));
                    cell2.BackgroundColor = rowColor;
                    cell2.Padding = 8;
                    cell2.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell2.BorderColor = borderColor;
                    table.AddCell(cell2);

                    // Cellule pour cocher avec bordure
                        PdfPCell cell3 = new PdfPCell(new Phrase("", normalFont));
                        cell3.BackgroundColor = rowColor;
                        cell3.Padding = 15;
                        cell3.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell3.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell3.BorderColor = darkGray;
                        cell3.BorderWidth = 1.5f;
                        table.AddCell(cell3);
                    }
                }
                else
                {
                    // Aucun produit
                    PdfPCell emptyCell = new PdfPCell(new Phrase("Aucun produit", normalFont));
                    emptyCell.Colspan = 3;
                    emptyCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    emptyCell.Padding = 15;
                    table.AddCell(emptyCell);
                }

                document.Add(table);
                document.Add(new Paragraph("\n\n"));

                // Zone pour signature stylisée
                PdfPTable signatureTable = new PdfPTable(1);
                signatureTable.WidthPercentage = 60;
                signatureTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                
                PdfPCell signatureCell = new PdfPCell();
                signatureCell.BackgroundColor = backgroundLight; // Blanco Roto (#F5F3EF)
                signatureCell.BorderColor = borderColor;
                signatureCell.BorderWidth = 1f;
                signatureCell.Padding = 15;
                Paragraph signature = new Paragraph();
                signature.Add(new Chunk("Signature du destinataire\n", FontFactory.GetFont(FontFactory.TIMES_BOLD, 11, primaryColor)));
                signature.Add(new Chunk("\n\n", normalFont));
                signature.Add(new Chunk("___________________________\n", FontFactory.GetFont(FontFactory.TIMES_ROMAN, 10, textMuted)));
                signature.Add(new Chunk("\nNom et prénom", FontFactory.GetFont(FontFactory.TIMES_ROMAN, 8, textMuted)));
                signatureCell.AddElement(signature);
                signatureTable.AddCell(signatureCell);
                
                document.Add(signatureTable);

                // Note en bas
                document.Add(new Paragraph("\n"));
                Paragraph note = new Paragraph("Veuillez vérifier l'état des produits avant de signer.", FontFactory.GetFont(FontFactory.TIMES_ITALIC, 9, textMuted));
                note.Alignment = Element.ALIGN_CENTER;
                document.Add(note);

                document.Close();

                byte[] byteArray = stream.ToArray();
                return File(byteArray, "application/pdf", "Bordereau_" + commande.NumeroCommande + ".pdf");
            }
        }

        // GET: Admin/Livraison
        public ActionResult Livraison(int page = 1)
        {
            using (var db = new ECommerceDbContext())
            {
                var modes = db.GetModesLivraison();
                var stats = db.GetLivraisonStats();
                
                // Pagination des zones de livraison (5 par page)
                int pageSize = 5;
                var zonesPaged = db.GetZonesLivraisonPaged(page, pageSize);
                
                ViewBag.Stats = stats;
                ViewBag.Zones = zonesPaged.Items;
                ViewBag.ZonesPaged = zonesPaged;
                ViewBag.CurrentPage = page;
                
                return View(modes);
            }
        }

        // POST: Admin/Livraison/Create
        [HttpPost]
        public ActionResult CreateModeLivraison(FormCollection form)
        {
            try
            {
                // Récupérer EstActif - peut être "true", "false", ou absent (checkbox non cochée)
                var estActifValue = form["EstActif"];
                bool estActif = !string.IsNullOrEmpty(estActifValue) && estActifValue.ToLower() == "true";
                
                var mode = new ModeLivraison
                {
                    Nom = form["Nom"],
                    Description = string.IsNullOrEmpty(form["Description"]) ? null : form["Description"],
                    Tarif = decimal.Parse(form["Tarif"] ?? "0"),
                    DelaiEstime = form["DelaiEstime"],
                    EstActif = estActif
                };

                if (string.IsNullOrWhiteSpace(mode.Nom) || mode.Tarif < 0 || string.IsNullOrWhiteSpace(mode.DelaiEstime))
                {
                    return Json(new { success = false, message = "Données invalides. Veuillez remplir tous les champs obligatoires." });
                }

                using (var db = new ECommerceDbContext())
                {
                    if (db.CreateModeLivraison(mode))
                    {
                        return Json(new { success = true, message = "Mode de livraison créé avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la création" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // POST: Admin/Livraison/Update
        [HttpPost]
        public ActionResult UpdateModeLivraison(FormCollection form)
        {
            try
            {
                if (string.IsNullOrEmpty(form["ModeLivraisonId"]) || !int.TryParse(form["ModeLivraisonId"], out int modeId))
                {
                    return Json(new { success = false, message = "ID invalide" });
                }

                // Récupérer EstActif - peut être "true", "false", ou absent (checkbox non cochée)
                var estActifValue = form["EstActif"];
                bool estActif = !string.IsNullOrEmpty(estActifValue) && estActifValue.ToLower() == "true";
                
                var mode = new ModeLivraison
                {
                    ModeLivraisonId = modeId,
                    Nom = form["Nom"],
                    Description = string.IsNullOrEmpty(form["Description"]) ? null : form["Description"],
                    Tarif = decimal.Parse(form["Tarif"] ?? "0"),
                    DelaiEstime = form["DelaiEstime"],
                    EstActif = estActif
                };

                if (string.IsNullOrWhiteSpace(mode.Nom) || mode.Tarif < 0 || string.IsNullOrWhiteSpace(mode.DelaiEstime))
                {
                    return Json(new { success = false, message = "Données invalides. Veuillez remplir tous les champs obligatoires." });
                }

                using (var db = new ECommerceDbContext())
                {
                    if (db.UpdateModeLivraison(mode))
                    {
                        return Json(new { success = true, message = "Mode de livraison mis à jour avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la mise à jour" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // POST: Admin/Livraison/Delete
        [HttpPost]
        public ActionResult DeleteModeLivraison(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "ID invalide" });
                }

                using (var db = new ECommerceDbContext())
                {
                    if (db.DeleteModeLivraison(id))
                    {
                        return Json(new { success = true, message = "Mode de livraison supprimé avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la suppression" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // GET: Admin/Livraison/GetMode
        [HttpGet]
        public ActionResult GetModeLivraison(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var mode = db.GetModeLivraison(id);
                if (mode == null)
                {
                    return Json(new { success = false, message = "Mode de livraison introuvable" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { 
                    success = true, 
                    mode = new {
                        ModeLivraisonId = mode.ModeLivraisonId,
                        Nom = mode.Nom,
                        Description = mode.Description,
                        Tarif = mode.Tarif,
                        DelaiEstime = mode.DelaiEstime,
                        EstActif = mode.EstActif
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================
        // ZONES DE LIVRAISON
        // ============================================

        // POST: Admin/Livraison/CreateZone
        [HttpPost]
        public ActionResult CreateZoneLivraison(FormCollection form)
        {
            try
            {
                var estActifValue = form["EstActif"];
                bool estActif = !string.IsNullOrEmpty(estActifValue) && estActifValue.ToLower() == "true";
                
                var zone = new ZoneLivraison
                {
                    ZoneVille = form["ZoneVille"],
                    Supplement = decimal.Parse(form["Supplement"] ?? "0"),
                    DelaiEstime = form["DelaiEstime"],
                    EstActif = estActif
                };

                if (string.IsNullOrWhiteSpace(zone.ZoneVille) || string.IsNullOrWhiteSpace(zone.DelaiEstime))
                {
                    return Json(new { success = false, message = "Données invalides. Veuillez remplir tous les champs obligatoires." });
                }

                using (var db = new ECommerceDbContext())
                {
                    if (db.CreateZoneLivraison(zone))
                    {
                        return Json(new { success = true, message = "Zone de livraison créée avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la création" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // POST: Admin/Livraison/UpdateZone
        [HttpPost]
        public ActionResult UpdateZoneLivraison(FormCollection form)
        {
            try
            {
                if (string.IsNullOrEmpty(form["ZoneLivraisonId"]) || !int.TryParse(form["ZoneLivraisonId"], out int zoneId))
                {
                    return Json(new { success = false, message = "ID invalide" });
                }

                var estActifValue = form["EstActif"];
                bool estActif = !string.IsNullOrEmpty(estActifValue) && estActifValue.ToLower() == "true";
                
                var zone = new ZoneLivraison
                {
                    ZoneLivraisonId = zoneId,
                    ZoneVille = form["ZoneVille"],
                    Supplement = decimal.Parse(form["Supplement"] ?? "0"),
                    DelaiEstime = form["DelaiEstime"],
                    EstActif = estActif
                };

                if (string.IsNullOrWhiteSpace(zone.ZoneVille) || string.IsNullOrWhiteSpace(zone.DelaiEstime))
                {
                    return Json(new { success = false, message = "Données invalides. Veuillez remplir tous les champs obligatoires." });
                }

                using (var db = new ECommerceDbContext())
                {
                    if (db.UpdateZoneLivraison(zone))
                    {
                        return Json(new { success = true, message = "Zone de livraison mise à jour avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la mise à jour" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // POST: Admin/Livraison/DeleteZone
        [HttpPost]
        public ActionResult DeleteZoneLivraison(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "ID invalide" });
                }

                using (var db = new ECommerceDbContext())
                {
                    if (db.DeleteZoneLivraison(id))
                    {
                        return Json(new { success = true, message = "Zone de livraison supprimée avec succès" });
                    }
                    return Json(new { success = false, message = "Erreur lors de la suppression" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur serveur : " + ex.Message });
            }
        }

        // GET: Admin/Livraison/GetZone
        [HttpGet]
        public ActionResult GetZoneLivraison(int id)
        {
            using (var db = new ECommerceDbContext())
            {
                var zone = db.GetZoneLivraison(id);
                if (zone == null)
                {
                    return Json(new { success = false, message = "Zone de livraison introuvable" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { 
                    success = true, 
                    zone = new {
                        ZoneLivraisonId = zone.ZoneLivraisonId,
                        ZoneVille = zone.ZoneVille,
                        Supplement = zone.Supplement,
                        DelaiEstime = zone.DelaiEstime,
                        EstActif = zone.EstActif
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Statistiques générales
                var stats = GetUserStatistics(connection);

                // Graphiques d'évolution
                var evolutionData = GetUserEvolutionData(connection);
                ViewBag.EvolutionData = evolutionData;

                // Utilisateurs les plus actifs
                var activeUsers = GetMostActiveUsers(connection);
                ViewBag.ActiveUsers = activeUsers;

                return View(stats);
            }
        }

        // GET: Admin/Utilisateurs
        public ActionResult Utilisateurs(string searchTerm = "", string statusFilter = "all", int page = 1, int pageSize = 20)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Stats cards
                var statsQuery = @"
                    SELECT 
                        COUNT(*) as TotalUsers,
                        SUM(CASE WHEN c.EstActif = 1 THEN 1 ELSE 0 END) as ActiveUsers,
                        SUM(CASE WHEN u.DateCreation >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN 1 ELSE 0 END) as ThisMonthUsers,
                        (SELECT COUNT(*) FROM Commandes) as TotalOrders
                    FROM Utilisateurs u
                    LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                    WHERE u.TypeUtilisateur = 'Client';";

                using (var statsCmd = new SqlCommand(statsQuery, connection))
                using (var reader = statsCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ViewBag.CardTotalUsers = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        ViewBag.CardActiveUsers = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        ViewBag.CardThisMonth = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        ViewBag.CardTotalOrders = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    }
                }

                var users = GetUsersList(connection, searchTerm, statusFilter, page, pageSize, out int totalCount);

                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return View(users);
            }
        }

        // GET: Admin/Utilisateurs/Details/5
        public ActionResult UserDetails(int id)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var user = GetUserDetails(connection, id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                return View(user);
            }
        }

        // POST: Admin/Utilisateurs/ToggleStatus
        [HttpPost]
        public ActionResult ToggleUserStatus(int userId, bool isActive)
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Non autorisé" });
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var updateQuery = @"UPDATE Clients 
                                       SET EstActif = @EstActif 
                                       WHERE ClientId = @ClientId";
                    
                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EstActif", isActive);
                        command.Parameters.AddWithValue("@ClientId", userId);
                        command.ExecuteNonQuery();
                    }

                    return Json(new { success = true, message = isActive ? "Compte activé avec succès" : "Compte désactivé avec succès" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erreur: " + ex.Message });
            }
        }

        // GET: Admin/Utilisateurs/GetStats
        [HttpGet]
        public ActionResult GetUserStats(string period = "month")
        {
            if (Session["TypeUtilisateur"]?.ToString() != "Admin")
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var stats = GetUserStatistics(connection, period);
                return Json(new
                {
                    TotalUsers = stats.TotalUsers,
                    ActiveUsers = stats.ActiveUsers,
                    InactiveUsers = stats.InactiveUsers,
                    NewUsers = stats.NewUsers,
                    ActiveThisPeriod = stats.ActiveThisPeriod,
                    ActivityRate = stats.ActivityRate
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper Methods
        private UserStatisticsViewModel GetUserStatistics(SqlConnection connection, string period = "month")
        {
            DateTime startDate;
            switch (period.ToLower())
            {
                case "day":
                    startDate = DateTime.Now.Date;
                    break;
                case "week":
                    startDate = DateTime.Now.AddDays(-7);
                    break;
                case "month":
                    startDate = DateTime.Now.AddMonths(-1);
                    break;
                default:
                    startDate = DateTime.Now.AddMonths(-1);
                    break;
            }

            var query = @"SELECT 
                            COUNT(*) as TotalUsers,
                            SUM(CASE WHEN c.EstActif = 1 THEN 1 ELSE 0 END) as ActiveUsers,
                            SUM(CASE WHEN c.EstActif = 0 THEN 1 ELSE 0 END) as InactiveUsers,
                            SUM(CASE WHEN u.DateCreation >= @StartDate THEN 1 ELSE 0 END) as NewUsers,
                            SUM(CASE WHEN c.DerniereConnexion >= @StartDate THEN 1 ELSE 0 END) as ActiveThisPeriod
                         FROM Utilisateurs u
                         LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                         WHERE u.TypeUtilisateur = 'Client'";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int totalUsers = reader.GetInt32(0);
                        int activeUsers = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        int inactiveUsers = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        int newUsers = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        int activeThisPeriod = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                        double activityRate = totalUsers > 0 ? (double)activeThisPeriod / totalUsers * 100 : 0;

                        return new UserStatisticsViewModel
                        {
                            TotalUsers = totalUsers,
                            ActiveUsers = activeUsers,
                            InactiveUsers = inactiveUsers,
                            NewUsers = newUsers,
                            ActiveThisPeriod = activeThisPeriod,
                            ActivityRate = Math.Round(activityRate, 2)
                        };
                    }
                }
            }

            return new UserStatisticsViewModel
            {
                TotalUsers = 0,
                ActiveUsers = 0,
                InactiveUsers = 0,
                NewUsers = 0,
                ActiveThisPeriod = 0,
                ActivityRate = 0.0
            };
        }

        private List<UserEvolutionViewModel> GetUserEvolutionData(SqlConnection connection)
        {
            var evolutionData = new List<UserEvolutionViewModel>();
            var startDate = DateTime.Now.AddMonths(-6);

            var query = @"SELECT 
                            CAST(u.DateCreation AS DATE) as Date,
                            COUNT(*) as Count
                         FROM Utilisateurs u
                         WHERE u.TypeUtilisateur = 'Client' 
                           AND u.DateCreation >= @StartDate
                         GROUP BY CAST(u.DateCreation AS DATE)
                         ORDER BY Date";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        evolutionData.Add(new UserEvolutionViewModel
                        {
                            Date = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                            Count = reader.GetInt32(1)
                        });
                    }
                }
            }

            return evolutionData;
        }

        private List<ActiveUserViewModel> GetMostActiveUsers(SqlConnection connection, int limit = 10)
        {
            var activeUsers = new List<ActiveUserViewModel>();

            var query = @"SELECT TOP (@Limit)
                            c.ClientId,
                            c.Prenom,
                            c.Nom,
                            u.Email,
                            c.DerniereConnexion,
                            (SELECT COUNT(*) FROM Commandes WHERE ClientId = c.ClientId) as OrderCount
                         FROM Clients c
                         INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                         WHERE c.DerniereConnexion IS NOT NULL
                         ORDER BY c.DerniereConnexion DESC";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Limit", limit);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activeUsers.Add(new ActiveUserViewModel
                        {
                            ClientId = reader.GetInt32(0),
                            Prenom = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nom = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Email = reader.GetString(3),
                            DerniereConnexion = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                            OrderCount = reader.GetInt32(5)
                        });
                    }
                }
            }

            return activeUsers;
        }

        private List<UserListViewModel> GetUsersList(SqlConnection connection, string searchTerm, string statusFilter, int page, int pageSize, out int totalCount)
        {
            var users = new List<UserListViewModel>();
            var offset = (page - 1) * pageSize;

            // Build WHERE clause
            var whereClause = "WHERE u.TypeUtilisateur = 'Client'";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClause += " AND (u.Email LIKE @SearchTerm OR c.Nom LIKE @SearchTerm OR c.Prenom LIKE @SearchTerm)";
                parameters.Add(new SqlParameter("@SearchTerm", "%" + searchTerm + "%"));
            }

            if (statusFilter == "active")
            {
                whereClause += " AND c.EstActif = 1";
            }
            else if (statusFilter == "inactive")
            {
                whereClause += " AND c.EstActif = 0";
            }

            // Get total count
            var countQuery = $@"SELECT COUNT(*) 
                               FROM Utilisateurs u
                               LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                               {whereClause}";

            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(param);
                }
                totalCount = (int)countCommand.ExecuteScalar();
            }

            // Get paginated results
            var query = $@"SELECT 
                            c.ClientId,
                            u.UtilisateurId,
                            c.Prenom,
                            c.Nom,
                            u.Email,
                            c.Telephone,
                            (SELECT TOP 1 Ville FROM Adresses a WHERE a.ClientId = c.ClientId ORDER BY a.EstParDefaut DESC, a.AdresseId DESC) as Ville,
                            (SELECT ISNULL(SUM(TotalTTC),0) FROM Commandes WHERE ClientId = c.ClientId) as TotalDepense,
                            c.EstActif,
                            u.DateCreation,
                            c.DerniereConnexion,
                            (SELECT COUNT(*) FROM Commandes WHERE ClientId = c.ClientId) as OrderCount
                         FROM Utilisateurs u
                         LEFT JOIN Clients c ON u.UtilisateurId = c.UtilisateurId
                         {whereClause}
                         ORDER BY u.DateCreation DESC
                         OFFSET @Offset ROWS
                         FETCH NEXT @PageSize ROWS ONLY";

            using (var command = new SqlCommand(query, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
                command.Parameters.Add(new SqlParameter("@Offset", offset));
                command.Parameters.Add(new SqlParameter("@PageSize", pageSize));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserListViewModel
                        {
                            ClientId = reader.GetInt32(0),
                            UtilisateurId = reader.GetInt32(1),
                            Prenom = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Nom = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Email = reader.GetString(4),
                            Telephone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Ville = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            TotalDepense = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                            EstActif = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                            DateCreation = reader.GetDateTime(9),
                            DerniereConnexion = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                            OrderCount = reader.GetInt32(11)
                        });
                    }
                }
            }

            return users;
        }

        private UserDetailsViewModel GetUserDetails(SqlConnection connection, int clientId)
        {
            var query = @"SELECT 
                            c.ClientId,
                            u.UtilisateurId,
                            c.Prenom,
                            c.Nom,
                            u.Email,
                            c.Telephone,
                            c.DateNaissance,
                            c.EstActif,
                            u.DateCreation,
                            c.DerniereConnexion,
                            (SELECT COUNT(*) FROM Commandes WHERE ClientId = c.ClientId) as OrderCount,
                            (SELECT SUM(TotalTTC) FROM Commandes WHERE ClientId = c.ClientId) as TotalSpent
                         FROM Clients c
                         INNER JOIN Utilisateurs u ON c.UtilisateurId = u.UtilisateurId
                         WHERE c.ClientId = @ClientId";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new UserDetailsViewModel
                        {
                            ClientId = reader.GetInt32(0),
                            UtilisateurId = reader.GetInt32(1),
                            Prenom = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Nom = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Email = reader.GetString(4),
                            Telephone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            DateNaissance = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                            EstActif = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                            DateCreation = reader.GetDateTime(8),
                            DerniereConnexion = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9),
                            OrderCount = reader.GetInt32(10),
                            TotalSpent = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11)
                        };
                    }
                }
            }

            return null;
        }
    }
}
