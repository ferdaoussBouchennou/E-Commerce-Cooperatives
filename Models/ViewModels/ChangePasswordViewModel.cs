using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Le mot de passe actuel est requis")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe actuel")]
        public string CurrentPassword { get; set; }
        
        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string NewPassword { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le nouveau mot de passe")]
        [Compare("NewPassword", ErrorMessage = "Le nouveau mot de passe et la confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }
    }
}

