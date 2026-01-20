using System;

namespace E_Commerce_Cooperatives.Models
{
    public class ZoneLivraison
    {
        public int ZoneLivraisonId { get; set; }
        public string ZoneVille { get; set; }
        public decimal Supplement { get; set; }
        public int DelaiMinStandard { get; set; } // Délai minimum en jours pour Standard
        public int DelaiMaxStandard { get; set; } // Délai maximum en jours pour Standard
        public int DelaiMinExpress { get; set; }  // Délai minimum en jours pour Express
        public int DelaiMaxExpress { get; set; }  // Délai maximum en jours pour Express
        public bool EstActif { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
