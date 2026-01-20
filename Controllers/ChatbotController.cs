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
                // Mots-clés déclencheurs de recherche (inclut typos et variations)
                string[] searchKeywords = { 
                    "cherche", "chercher", "trouve", "trouver", "achat", "acheter", "achteer", "achete", 
                    "prix", "coût", "combien", "produit", "article", "disponible", "vendre", "vends",
                    "huile", "savon", "tapis", "poterie", "miel", "argan", "sac", "cabas", "cuir", "vêtement" 
                };
                
                // On considère qu'il y a intention de recherche si un mot-clé est présent 
                // OU si le message est assez long et contient des noms potentiels
                bool isSearchIntent = searchKeywords.Any(k => message.ToLower().Contains(k)) || message.Split(' ').Length > 3;

                if (isSearchIntent)
                {
                    // Recherche dans la base de données
                    var stopWords = new[] { "je", "j'", "tu", "il", "nous", "vous", "le", "la", "les", "un", "une", "des", "de", "du", "d'", "l'", "ce", "cette", "ces", "et", "ou", "mais", "est", "sont", "a", "ont", "svp", "merci", "bonjour", "pour", "avec", "dans", "sur" };
                    
                    var queryTerms = message.ToLower()
                        .Split(new[] { ' ', '\'', ',', '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => !stopWords.Contains(w) && w.Length > 2)
                        .ToList();

                    var foundProducts = new System.Collections.Generic.List<dynamic>();

                    if (queryTerms.Any())
                    {
                        // On récupère les produits avec leurs catégories pour une recherche plus riche
                        // Note: On limite la recherche directement en SQL pour plus d'efficacité
                        using (var connection = new System.Data.SqlClient.SqlConnection(db.GetConnectionString()))
                        {
                            await connection.OpenAsync();
                            var sql = @"
                                SELECT TOP 5 p.ProduitId, p.Nom, p.Description, p.Prix, p.StockTotal, c.Nom as CategorieNom
                                FROM Produits p
                                LEFT JOIN Categories c ON p.CategorieId = c.CategorieId
                                WHERE p.EstDisponible = 1 AND (";
                            
                            var orConditions = new System.Collections.Generic.List<string>();
                            var parameters = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
                            
                            for (int i = 0; i < queryTerms.Count; i++)
                            {
                                string paramName = "@term" + i;
                                orConditions.Add($"(p.Nom LIKE {paramName} OR p.Description LIKE {paramName} OR c.Nom LIKE {paramName})");
                                parameters.Add(new System.Data.SqlClient.SqlParameter(paramName, "%" + queryTerms[i] + "%"));
                            }
                            
                            sql += string.Join(" OR ", orConditions) + ")";
                            
                            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, connection))
                            {
                                cmd.Parameters.AddRange(parameters.ToArray());
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        foundProducts.Add(new {
                                            ProduitId = reader["ProduitId"],
                                            Nom = reader["Nom"],
                                            Description = reader["Description"],
                                            Prix = reader["Prix"],
                                            Stock = reader["StockTotal"],
                                            CategorieNom = reader["CategorieNom"]
                                        });
                                    }
                                }
                            }
                        }
                    }

                    if (foundProducts.Any())
                    {
                        context += "\n[CONTEXTE PRODUITS TROUVÉS DANS LA BASE DE DONNÉES]:\n";
                        foreach (var p in foundProducts)
                        {
                            context += $"- Produit: {p.Nom} (Catégorie: {p.CategorieNom}), Prix: {p.Prix} DH, Stock: {p.Stock}. Lien: <a href='/Produit/Details/{p.ProduitId}'>{p.Nom}</a>\n";
                        }
                        context += "\nINSTRUCTION: Utilisez ces informations pour répondre. Si vous recommandez un produit, vous DEVEZ utiliser le format de lien HTML fourni EXACTEMENT comme indiqué (<a href=...>...</a>). Donnez des détails sur le produit pour aider l'acheteur.\n";
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
