using System;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class ActiveUserViewModel
    {
        public int ClientId { get; set; }
        public string Prenom { get; set; }
        public string Nom { get; set; }
        public string Email { get; set; }
        public DateTime? DerniereConnexion { get; set; }
        public int OrderCount { get; set; }
    }
}

