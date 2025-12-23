using System;
using System.Collections.Generic;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class OrderConfirmationViewModel
    {
        public string NumeroCommande { get; set; }
        public DateTime DateCommande { get; set; }
        public decimal TotalTTC { get; set; }
        public string Statut { get; set; }
        public Adresse AdresseLivraison { get; set; }
        public ModeLivraison ModeLivraison { get; set; }
        public List<CommandeItem> Items { get; set; }
    }
}

