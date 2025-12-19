using System;

namespace E_Commerce_Cooperatives.Models
{
    public class LivraisonSuivi
    {
        public int SuiviId { get; set; }
        public int CommandeId { get; set; }
        public string Statut { get; set; }
        public string Description { get; set; }
        public string NumeroSuivi { get; set; }
        public DateTime DateStatut { get; set; }

        // Navigation property
        public Commande Commande { get; set; }
    }
}
