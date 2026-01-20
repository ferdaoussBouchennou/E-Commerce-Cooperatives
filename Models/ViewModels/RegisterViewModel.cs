using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "L'adresse email est requise")]
        [EmailAddress(ErrorMessage = "Veuillez entrer une adresse email valide")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Veuillez entrer un numéro de téléphone valide")]
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string MotDePasse { get; set; }

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("MotDePasse", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmerMotDePasse { get; set; }


    }
}

