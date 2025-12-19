using System;
using System.Collections.Generic;

namespace E_Commerce_Cooperatives.Models
{
    public class Client
    {
        public int ClientId { get; set; }
        public int UtilisateurId { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Telephone { get; set; }
        public DateTime? DateNaissance { get; set; }
        public bool EstActif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DerniereConnexion { get; set; }
        public string Email { get; set; }
        
        public string NomComplet => $"{Prenom} {Nom}";
    }
}
