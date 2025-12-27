using System;

namespace E_Commerce_Cooperatives.Models
{
    public class ModeLivraison
    {
        public int ModeLivraisonId { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public decimal Tarif { get; set; } // Prix de base du mode (33 MAD pour Standard, 60 MAD pour Express)
        public bool EstActif { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
