using System;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class AddressViewModel
    {
        public int AdresseId { get; set; }
        
        [Required(ErrorMessage = "L'adresse complète est requise")]
        [Display(Name = "Adresse complète")]
        public string AdresseComplete { get; set; }
        
        [Required(ErrorMessage = "La ville est requise")]
        [Display(Name = "Ville")]
        public string Ville { get; set; }
        
        [Display(Name = "Code postal")]
        public string CodePostal { get; set; }
        
        [Display(Name = "Pays")]
        public string Pays { get; set; }
        
        [Display(Name = "Adresse par défaut")]
        public bool EstParDefaut { get; set; }
        
        public DateTime DateCreation { get; set; }
    }
}

