using System;

namespace E_Commerce_Cooperatives.Models
{
    public class ZoneLivraison
    {
        public int ZoneLivraisonId { get; set; }
        public string ZoneVille { get; set; }
        public decimal Supplement { get; set; }
        public string DelaiEstime { get; set; }
        public bool EstActif { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
