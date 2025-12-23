using System;

namespace E_Commerce_Cooperatives.Models
{
    public class Favori
    {
        public int FavoriId { get; set; }
        public int ClientId { get; set; }
        public int ProduitId { get; set; }
        public DateTime DateAjout { get; set; }

        // Navigation properties
        public virtual Produit Produit { get; set; }
    }
}
