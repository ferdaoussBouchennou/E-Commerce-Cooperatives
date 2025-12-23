using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class AccountPreferencesViewModel
    {
        [Display(Name = "Recevoir des notifications par email")]
        public bool RecevoirNotificationsEmail { get; set; }
        
        [Display(Name = "Recevoir des offres promotionnelles")]
        public bool RecevoirOffresPromotionnelles { get; set; }
        
        [Display(Name = "Recevoir des newsletters")]
        public bool RecevoirNewsletters { get; set; }
        
        [Display(Name = "Langue préférée")]
        public string LanguePreferee { get; set; }
    }
}

