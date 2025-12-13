using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models
{
    public class Categorie
    {
        public int CategorieId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nom { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(500)]
        public string ImageUrl { get; set; }
        
        public bool EstActive { get; set; }
        
        public DateTime DateCreation { get; set; }
        
        public virtual ICollection<Produit> Produits { get; set; }
        
        public Categorie()
        {
            Produits = new HashSet<Produit>();
        }
    }
}

