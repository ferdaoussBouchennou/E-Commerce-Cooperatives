using System;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int ClientId { get; set; }
        
        [Required(ErrorMessage = "Le prénom est requis")]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }
        
        [Required(ErrorMessage = "Le nom est requis")]
        [Display(Name = "Nom")]
        public string Nom { get; set; }
        
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; }
        
        [Display(Name = "Date de naissance")]
        [DataType(DataType.Date)]
        public DateTime? DateNaissance { get; set; }
        
        public DateTime DateCreation { get; set; }
        public DateTime? DerniereConnexion { get; set; }
    }
}

