using System.Threading.Tasks;
using System.Linq;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiService _geminiService;
        private readonly ECommerceDbContext db = new ECommerceDbContext();

        public ChatbotController()
        {
            _geminiService = new GeminiService();
        }

        [HttpPost]
        public async Task<JsonResult> SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, error = "Message vide" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // Optionnel: Ajouter du contexte basé sur l'utilisateur connecté
                string context = "";
                if (Session["UserId"] != null)
                {
                    context += "L'utilisateur est connecté. ";
                }

                // --- LOGIQUE RAG (Recherche Intelligente) ---
                // Mots-clés déclencheurs de recherche
                string[] searchKeywords = { "cherche", "chercher", "trouve", "trouver", "achat", "acheter", "prix", "coût", "combien", "produit", "article", "huile", "savon", "tapis", "poterie", "miel" };
                
                bool isSearchIntent = searchKeywords.Any(k => message.ToLower().Contains(k));

                if (isSearchIntent)
                {
                    // Recherche dans la base de données
                    // Tokenisation de la requête utilisateur pour améliorer la recherche
                    var stopWords = new[] { "je", "j'", "tu", "il", "nous", "vous", "le", "la", "les", "un", "une", "des", "de", "du", "d'", "l'", "ce", "cette", "ces", "et", "ou", "mais", "est", "sont", "a", "ont", "cherche", "chercher", "trouver", "voudrais", "veux", "achat", "acheter", "prix", "coût", "combien", "svp", "merci", "bonjour", "pour", "avec" };
                    
                    var queryTerms = message.ToLower()
                        .Split(new[] { ' ', '\'', ',', '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => !stopWords.Contains(w) && w.Length > 2)
                        .ToList();

                    var foundProducts = new System.Collections.Generic.List<dynamic>();

                    if (queryTerms.Any())
                    {
                        // On récupère TOUS les produits en mémoire car linq to entities ne supportera pas queryTerms.Any
                        // Avec une petite base de données c'est acceptable. Sinon, faire une boucle ou PredicateBuilder.
                        var allProducts = db.Produits
                            .Select(p => new { p.ProduitId, p.Nom, p.Description, p.Prix, Stock = p.StockTotal, CategorieNom = p.Categorie != null ? p.Categorie.Nom : "" })
                            .ToList();

                        foundProducts = allProducts
                            .Where(p => queryTerms.Any(t => 
                                (p.Nom != null && p.Nom.ToLower().Contains(t)) || 
                                (p.Description != null && p.Description.ToLower().Contains(t)) ||
                                (p.CategorieNom != null && p.CategorieNom.ToLower().Contains(t))))
                            .Take(5)
                            .Cast<dynamic>()
                            .ToList();
                    }

                    if (foundProducts.Any())
                    {
                        context += "\n[CONTEXTE PRODUITS TROUVÉS DANS LA BASE DE DONNÉES]:\n";
                        foreach (var p in foundProducts)
                        {
                            context += $"- Produit: {p.Nom} (ID: {p.ProduitId}), Prix: {p.Prix} DH, Stock: {p.Stock}. Lien: <a href='/Produit/Details/{p.ProduitId}'>{p.Nom}</a>\n";
                        }
                        context += "\nINSTRUCTION: Utilisez ces informations pour répondre. Si vous recommandez un produit, vous DEVEZ utiliser le format de lien HTML fourni EXACTEMENT comme indiqué (<a href=...>...</a>). Ne créez pas de liens markdown [].\n";
                    }
                }

                // Appel asynchrone à l'API Gemini avec le contexte enrichi
                string response = await _geminiService.GetChatbotResponse(message, context);

                return Json(new { success = true, response = response }, JsonRequestBehavior.AllowGet);
            }
            catch (System.AggregateException aex)
            {
                var innerEx = aex.InnerException ?? aex;
                return Json(new { success = false, error = "Erreur API: " + innerEx.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, error = "Erreur serveur: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
