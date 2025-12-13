using System;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models
{
    public class Variante
    {
        public int VarianteId { get; set; }
        
        public int ProduitId { get; set; }
        public virtual Produit Produit { get; set; }
        
        [StringLength(50)]
        public string Taille { get; set; }
        
        [StringLength(50)]
        public string Couleur { get; set; }
        
        public int Stock { get; set; }
        
        public decimal PrixSupplementaire { get; set; }
        
        [StringLength(100)]
        public string SKU { get; set; }
        
        public bool EstDisponible { get; set; }
        
        public DateTime DateCreation { get; set; }
    }
}

