using System;
using System.Collections.Generic;

namespace E_Commerce_Cooperatives.Models
{
    public class Commande
    {
        public int CommandeId { get; set; }
        public string NumeroCommande { get; set; }
        public int ClientId { get; set; }
        public int AdresseId { get; set; }
        public int ModeLivraisonId { get; set; }
        public DateTime DateCommande { get; set; }
        public decimal FraisLivraison { get; set; }
        public decimal TotalHT { get; set; }
        public decimal MontantTVA { get; set; }
        public decimal TotalTTC { get; set; }
        public string Statut { get; set; } // Validée, Préparation, Expédiée, Livrée, Annulée
        public string Commentaire { get; set; }
        public DateTime? DateAnnulation { get; set; }
        public string RaisonAnnulation { get; set; }

        // Navigation properties
        public Client Client { get; set; }
        public Adresse Adresse { get; set; }
        public ModeLivraison ModeLivraison { get; set; }
        public List<CommandeItem> Items { get; set; }
        public List<LivraisonSuivi> SuiviLivraison { get; set; }
    }
}
