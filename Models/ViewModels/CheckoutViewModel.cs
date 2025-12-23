using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // Delivery Address
        [Required(ErrorMessage = "Le prénom est requis")]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Veuillez entrer une adresse email valide")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le téléphone est requis")]
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; }

        [Required(ErrorMessage = "L'adresse complète est requise")]
        [Display(Name = "Adresse complète")]
        public string AdresseComplete { get; set; }

        [Required(ErrorMessage = "La ville est requise")]
        [Display(Name = "Ville")]
        public string Ville { get; set; }

        [Required(ErrorMessage = "Le code postal est requis")]
        [Display(Name = "Code postal")]
        public string CodePostal { get; set; }

        [Display(Name = "Notes (optionnel)")]
        public string Notes { get; set; }

        // Delivery Method
        [Required(ErrorMessage = "Veuillez sélectionner un mode de livraison")]
        [Display(Name = "Mode de livraison")]
        public int ModeLivraisonId { get; set; }

        // Cart Items
        public List<CartItemViewModel> CartItems { get; set; }

        // Delivery Options
        public List<ModeLivraison> ModesLivraison { get; set; }

        // Totals
        public decimal SousTotal { get; set; }
        public decimal MontantTVA { get; set; }
        public decimal FraisLivraison { get; set; }
        public decimal TotalTTC { get; set; }
    }

    public class CartItemViewModel
    {
        public int ProduitId { get; set; }
        public int? VarianteId { get; set; }
        public string Nom { get; set; }
        public string ImageUrl { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal TotalLigne { get; set; }
    }
}

