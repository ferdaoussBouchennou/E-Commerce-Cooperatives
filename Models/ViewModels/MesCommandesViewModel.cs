using System.Collections.Generic;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class MesCommandesViewModel
    {
        public List<Commande> Commandes { get; set; }
        public Commande CommandeDetail { get; set; }
        public int? SelectedCommandeId { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool IsHistory { get; set; }
    }
}

