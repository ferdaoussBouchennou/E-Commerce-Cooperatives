using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using E_Commerce_Cooperatives.Models;
using System.Web;
using System.Web.Hosting;

namespace E_Commerce_Cooperatives.Helpers
{
    public static class DeliverySlipHelper
    {
        public static byte[] GenerateDeliverySlip(Commande commande)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with A4 size and margins
                Document document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Define Colors
                BaseColor primaryColor = new BaseColor(48, 92, 125); // #305C7D (Reflects brand)
                BaseColor accentColor = new BaseColor(255, 255, 255); // White
                BaseColor lightGray = new BaseColor(245, 247, 250); 
                BaseColor textColor = new BaseColor(60, 60, 60); 
                BaseColor borderColor = new BaseColor(230, 230, 230); 

                // Fonts - GEORGIA (Consistent with Invoice)
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "georgia.ttf");
                BaseFont baseFont;
                if (File.Exists(fontPath))
                {
                    baseFont = BaseFont.CreateFont(fontPath, BaseFont.CP1252, BaseFont.EMBEDDED);
                }
                else
                {
                    baseFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                }
                
                Font titleFont = new Font(baseFont, 24, Font.NORMAL, primaryColor);
                Font headerLabelFont = new Font(baseFont, 10, Font.BOLD, primaryColor);
                Font headerValueFont = new Font(baseFont, 11, Font.NORMAL, textColor);
                Font tableHeaderFont = new Font(baseFont, 10, Font.BOLD, accentColor);
                Font tableRowFont = new Font(baseFont, 10, Font.NORMAL, textColor);

                // --- HEADER WITH LOGO ---
                PdfPTable topTable = new PdfPTable(1);
                topTable.WidthPercentage = 100;
                topTable.DefaultCell.Border = Rectangle.NO_BORDER;

                // Logo
                try 
                {
                    string logoPath = HostingEnvironment.MapPath("~/Content/images/logo-coop.png");
                    if (File.Exists(logoPath))
                    {
                        Image logo = Image.GetInstance(logoPath);
                        logo.ScaleToFit(150f, 80f); 
                        logo.Alignment = Element.ALIGN_CENTER;
                        
                        PdfPCell logoCell = new PdfPCell(logo);
                        logoCell.Border = Rectangle.NO_BORDER;
                        logoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        logoCell.PaddingBottom = 15f;
                        topTable.AddCell(logoCell);
                    }
                }
                catch { }

                // Title
                PdfPCell titleCell = new PdfPCell(new Phrase("BORDEREAU DE LIVRAISON", titleFont));
                titleCell.Border = Rectangle.NO_BORDER;
                titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
                titleCell.PaddingBottom = 20f;
                topTable.AddCell(titleCell);

                document.Add(topTable);
                document.Add(new Paragraph("\n"));

                // --- INFO SECTION (Order & Shipping) ---
                PdfPTable infoTable = new PdfPTable(2);
                infoTable.WidthPercentage = 100;
                infoTable.SetWidths(new float[] { 1f, 1f });
                
                // ORDER INFO (Left)
                PdfPCell orderInfoCell = new PdfPCell();
                orderInfoCell.Border = Rectangle.NO_BORDER;
                orderInfoCell.PaddingRight = 20f;

                AddInfoLine(orderInfoCell, "Numéro de commande:", commande.NumeroCommande, headerLabelFont, headerValueFont, primaryColor);
                AddInfoLine(orderInfoCell, "Date:", commande.DateCommande.ToString("dd MMMM yyyy"), headerLabelFont, headerValueFont);
                AddInfoLine(orderInfoCell, "Mode de livraison:", commande.ModeLivraison != null ? commande.ModeLivraison.Nom : "Standard", headerLabelFont, headerValueFont);
                
                if (!string.IsNullOrEmpty(commande.Commentaire))
                {
                    AddInfoLine(orderInfoCell, "Instructions:", commande.Commentaire, headerLabelFont, headerValueFont);
                }

                infoTable.AddCell(orderInfoCell);

                // CLIENT / SHIPPING INFO (Right)
                PdfPCell clientInfoCell = new PdfPCell();
                clientInfoCell.Border = Rectangle.NO_BORDER;
                clientInfoCell.PaddingLeft = 20f;
                
                clientInfoCell.AddElement(new Paragraph("CLIENT", headerLabelFont));
                clientInfoCell.AddElement(new Paragraph($"{commande.Client.Prenom} {commande.Client.Nom}", new Font(baseFont, 12, Font.BOLD, textColor)));
                
                if (!string.IsNullOrEmpty(commande.Client.Telephone))
                     clientInfoCell.AddElement(new Paragraph(commande.Client.Telephone, headerValueFont));

                if (commande.Adresse != null)
                {
                    clientInfoCell.AddElement(new Paragraph("\n"));
                    clientInfoCell.AddElement(new Paragraph("ADRESSE DE LIVRAISON:", headerLabelFont));
                    clientInfoCell.AddElement(new Paragraph(commande.Adresse.AdresseComplete, headerValueFont));
                    clientInfoCell.AddElement(new Paragraph($"{commande.Adresse.Ville}, {commande.Adresse.CodePostal}", headerValueFont));
                }
                
                infoTable.AddCell(clientInfoCell);
                document.Add(infoTable);

                document.Add(new Paragraph("\n\n"));

                // --- ITEMS TABLE (No Prices) ---
                PdfPTable itemsTable = new PdfPTable(2);
                itemsTable.WidthPercentage = 100;
                itemsTable.SetWidths(new float[] { 4f, 1f }); // Description, Qty
                itemsTable.HeaderRows = 1;

                // Headers
                AddHeaderCell(itemsTable, "Description / Produit", tableHeaderFont, primaryColor);
                AddHeaderCell(itemsTable, "Quantité Livrée", tableHeaderFont, primaryColor, Element.ALIGN_CENTER);

                // Rows
                bool alternate = false;
                foreach (var item in commande.Items)
                {
                    BaseColor rowColor = alternate ? lightGray : BaseColor.WHITE;
                    AddRowCell(itemsTable, item.Produit != null ? item.Produit.Nom : "Produit", tableRowFont, rowColor, borderColor);
                    AddRowCell(itemsTable, item.Quantite.ToString(), tableRowFont, rowColor, borderColor, Element.ALIGN_CENTER);
                    alternate = !alternate;
                }

                // Add empty rows for manual checking if needed
                // for (int i = 0; i < 3; i++)
                // {
                //      BaseColor rowColor = alternate ? lightGray : BaseColor.WHITE;
                //      AddRowCell(itemsTable, " ", tableRowFont, rowColor, borderColor); 
                //      AddRowCell(itemsTable, " ", tableRowFont, rowColor, borderColor);
                //      alternate = !alternate;
                // }

                document.Add(itemsTable);
                document.Add(new Paragraph("\n"));
                
                // --- SIGNATURE SECTION ---
                document.Add(new Paragraph("\n\n\n"));
                PdfPTable signatureTable = new PdfPTable(2);
                signatureTable.WidthPercentage = 100;
                
                PdfPCell sigCell1 = new PdfPCell(new Paragraph("Signature du Livreur:", headerLabelFont));
                sigCell1.Border = Rectangle.NO_BORDER;
                signatureTable.AddCell(sigCell1);
                
                PdfPCell sigCell2 = new PdfPCell(new Paragraph("Signature du Client (Reçu):", headerLabelFont));
                sigCell2.Border = Rectangle.NO_BORDER;
                signatureTable.AddCell(sigCell2);
                
                document.Add(signatureTable);

                // --- FOOTER ---
                document.Add(new Paragraph("\n\n\n\n"));
                PdfPTable footerTable = new PdfPTable(1);
                footerTable.WidthPercentage = 100;
                
                PdfPCell footerCell = new PdfPCell(new Phrase("Merci pour votre confiance !", new Font(baseFont, 10, Font.ITALIC, new BaseColor(100, 100, 100))));
                footerCell.Border = Rectangle.TOP_BORDER;
                footerCell.BorderColor = borderColor;
                footerCell.PaddingTop = 15f;
                footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                footerTable.AddCell(footerCell);
                
                document.Add(footerTable);

                document.Close();
                return ms.ToArray();
            }
        }

        private static void AddInfoLine(PdfPCell container, string label, string value, Font labelFont, Font valueFont, BaseColor valueColor = null)
        {
            PdfPTable table = new PdfPTable(1);
            table.WidthPercentage = 100;
            table.DefaultCell.Border = Rectangle.NO_BORDER;

            PdfPCell c = new PdfPCell();
            c.Border = Rectangle.NO_BORDER;
            c.AddElement(new Paragraph(label, labelFont));
            
            Font vFont = valueFont;
            if (valueColor != null) vFont = new Font(valueFont.BaseFont, valueFont.Size + 2, Font.BOLD, valueColor);

            c.AddElement(new Paragraph(value, vFont));
            c.PaddingBottom = 8f;
            
            table.AddCell(c);
            container.AddElement(table);
        }

        private static void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bg, int align = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bg;
            cell.HorizontalAlignment = align;
            cell.Padding = 10f; 
            cell.Border = Rectangle.NO_BORDER;
            table.AddCell(cell);
        }

        private static void AddRowCell(PdfPTable table, string text, Font font, BaseColor bg, BaseColor borderColor, int align = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bg;
            cell.HorizontalAlignment = align;
            cell.Padding = 10f;
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColor = borderColor;
            table.AddCell(cell);
        }
    }
}
