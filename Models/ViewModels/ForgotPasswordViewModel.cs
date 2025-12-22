using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; }
    }
}

