using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models
{
    public class Produit
    {
        public int ProduitId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Nom { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public decimal Prix { get; set; }
        
        [StringLength(500)]
        public string ImageUrl { get; set; }
        
        public int? CategorieId { get; set; }
        public virtual Categorie Categorie { get; set; }
        
        public int? CooperativeId { get; set; }
        public virtual Cooperative Cooperative { get; set; }
        
        public int StockTotal { get; set; }
        
        public int SeuilAlerte { get; set; }
        
        public bool EstDisponible { get; set; }
        
        public bool EstEnVedette { get; set; }
        
        public bool EstNouveau { get; set; }
        
        public DateTime DateCreation { get; set; }
        
        public DateTime? DateModification { get; set; }
        
        public virtual ICollection<ImageProduit> Images { get; set; }
        public virtual ICollection<Variante> Variantes { get; set; }
        
        // Propriétés calculées pour la vue
        public decimal NoteMoyenne { get; set; }
        public int NombreAvis { get; set; }
        
        public Produit()
        {
            Images = new HashSet<ImageProduit>();
            Variantes = new HashSet<Variante>();
        }
    }
}

