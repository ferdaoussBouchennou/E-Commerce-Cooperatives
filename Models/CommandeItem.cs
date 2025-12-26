using System;

namespace E_Commerce_Cooperatives.Models
{
    public class CommandeItem
    {
        public int CommandeItemId { get; set; }
        public int CommandeId { get; set; }
        public int ProduitId { get; set; }
        public int? VarianteId { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; } // Prix HT stocké en base (ou TTC pour les anciennes commandes)
        public decimal TotalLigne { get; set; } // Total HT stocké en base (ou TTC pour les anciennes commandes)

        // Propriétés calculées pour l'affichage TTC
        // Détection automatique : si le prix stocké correspond au prix TTC du produit, c'est une ancienne commande
        // Sinon, on suppose que c'est HT (nouvelles commandes) et on applique la TVA
        public decimal PrixUnitaireTTC 
        { 
            get 
            {
                // Si on a accès au produit, on peut détecter si le prix est déjà TTC
                if (Produit != null)
                {
                    decimal prixHTProduit = Produit.Prix;
                    decimal prixTTCAttendu = Math.Round(prixHTProduit * 1.20m, 2);
                    
                    // Si le prix stocké est proche du prix TTC attendu (tolérance de 0.50 pour gérer les variantes),
                    // c'est une ancienne commande où le prix TTC était stocké directement
                    if (Math.Abs(PrixUnitaire - prixTTCAttendu) < 0.50m && PrixUnitaire > prixHTProduit)
                    {
                        // Le prix est déjà TTC, on le retourne tel quel
                        return Math.Round(PrixUnitaire, 2);
                    }
                }
                // Sinon, on suppose que c'est HT (nouvelles commandes) et on applique la TVA
                return Math.Round(PrixUnitaire * 1.20m, 2);
            } 
        }
        
        public decimal TotalLigneTTC 
        { 
            get 
            {
                // Utiliser PrixUnitaireTTC pour être cohérent
                return Math.Round(PrixUnitaireTTC * Quantite, 2);
            } 
        }

        // Navigation properties
        public Commande Commande { get; set; }
        public Produit Produit { get; set; }
        public Variante Variante { get; set; }
    }
}
