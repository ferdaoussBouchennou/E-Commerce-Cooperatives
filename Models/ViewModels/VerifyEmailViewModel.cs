using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Le code de vérification est requis")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Le code doit contenir 6 chiffres")]
        [Display(Name = "Code de vérification")]
        public string VerificationCode { get; set; }

        public string Email { get; set; }
    }
}

