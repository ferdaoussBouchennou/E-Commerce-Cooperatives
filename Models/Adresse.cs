using System;

namespace E_Commerce_Cooperatives.Models
{
    public class Adresse
    {
        public int AdresseId { get; set; }
        public int ClientId { get; set; }
        public string AdresseComplete { get; set; }
        public string Ville { get; set; }
        public string CodePostal { get; set; }
        public string Pays { get; set; }
        public bool EstParDefaut { get; set; }
        public DateTime DateCreation { get; set; }
        
        public Client Client { get; set; }
    }
}
