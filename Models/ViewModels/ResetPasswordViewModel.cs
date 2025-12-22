using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password", ErrorMessage = "Le mot de passe et la confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }
    }
}

