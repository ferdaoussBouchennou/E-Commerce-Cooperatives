using System;
using System.Collections.Generic;

namespace E_Commerce_Cooperatives.Models
{
    public class Utilisateur
    {
        public int UtilisateurId { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
        public string TypeUtilisateur { get; set; }
        public DateTime DateCreation { get; set; }
        
        // Related client data (if TypeUtilisateur == "Client")
        public Client Client { get; set; }
    }
}

