namespace E_Commerce_Cooperatives.Models
{
    // Helper class for order creation
    public class CartItemForOrder
    {
        public int ProduitId { get; set; }
        public int? VarianteId { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string Nom { get; set; }
    }
}
