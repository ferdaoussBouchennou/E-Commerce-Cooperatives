using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_Cooperatives.Models
{
    public class AvisProduit
    {
        public int AvisId { get; set; }
        
        public int ClientId { get; set; }
        
        public int ProduitId { get; set; }
        public virtual Produit Produit { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Note { get; set; }
        
        [StringLength(1000)]
        public string Commentaire { get; set; }
        
        public DateTime DateAvis { get; set; }
        
        // Propriété calculée pour la vue (non mappée à la base de données)
        public string ClientNom { get; set; }
    }
}

