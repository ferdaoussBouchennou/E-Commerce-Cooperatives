using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_Cooperatives.Models
{
    public class ImageProduit
    {
        public int ImageId { get; set; }
        
        public int ProduitId { get; set; }
        public virtual Produit Produit { get; set; }
        
        [Required]
        [StringLength(500)]
        public string UrlImage { get; set; }
        
        public bool EstPrincipale { get; set; }
        
        public DateTime DateAjout { get; set; }
    }
}

