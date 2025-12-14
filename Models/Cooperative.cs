using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models
{
    public class Cooperative
    {
        public int CooperativeId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Nom { get; set; }
        
        public string Description { get; set; }
        
        [StringLength(500)]
        public string Adresse { get; set; }
        
        [StringLength(100)]
        public string Ville { get; set; }
        
        [StringLength(20)]
        public string Telephone { get; set; }
        
        [StringLength(500)]
        public string Logo { get; set; }
        
        public bool EstActive { get; set; }
        
        public DateTime DateCreation { get; set; }
        
        public virtual ICollection<Produit> Produits { get; set; }
        
        // Propriété calculée pour la vue (non mappée à la base de données)
        public int ProductCount { get; set; }
        
        public Cooperative()
        {
            Produits = new HashSet<Produit>();
        }
    }
}

