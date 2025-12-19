namespace E_Commerce_Cooperatives.Models
{
    public class CommandeItem
    {
        public int CommandeItemId { get; set; }
        public int CommandeId { get; set; }
        public int ProduitId { get; set; }
        public int? VarianteId { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal TotalLigne { get; set; }

        // Navigation properties
        public Commande Commande { get; set; }
        public Produit Produit { get; set; }
        public Variante Variante { get; set; }
    }
}
