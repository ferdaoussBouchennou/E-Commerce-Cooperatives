using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace E_Commerce_Cooperatives.Models
{
    public class GeminiService
    {
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;
        public GeminiService()
        {
            _apiKey = Environment.GetEnvironmentVariable("GeminiApiKey") ?? ConfigurationManager.AppSettings["GeminiApiKey"];
            _apiUrl = Environment.GetEnvironmentVariable("GeminiApiUrl") ?? ConfigurationManager.AppSettings["GeminiApiUrl"];
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }
        public async Task<string> GetChatbotResponse(string userMessage, string context = "")
        {
            try
            {
                // Vérifier la clé API
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return "Erreur: Clé API Gemini non configurée dans Web.config";
                }

                // Construire le prompt avec le contexte complet de Cooporia
                string systemPrompt = @"Tu es l'assistant virtuel de Cooporia, une plateforme e-commerce marocaine spécialisée dans les produits des coopératives locales.

🏪 À PROPOS DE Cooporia:
Cooporia est une plateforme qui connecte les consommateurs avec des produits authentiques issus de coopératives marocaines. Notre mission est de soutenir l'économie locale et promouvoir les produits traditionnels de qualité.

📦 CATÉGORIES DE PRODUITS:
- Produits Alimentaires: Huiles d'argan, miel, épices, confitures artisanales, fruits secs
- Cosmétiques Naturels: Savons traditionnels, huiles essentielles, produits à base d'argan
- Artisanat: Poterie, tapis, vannerie, articles en cuir
- Textiles: Vêtements traditionnels, tissus berbères, accessoires

🛒 PROCESSUS DE COMMANDE:
1. Parcourir le catalogue par catégories
2. Ajouter des produits au panier
3. Consulter le panier et modifier les quantités
4. Passer à la caisse et remplir les informations de livraison
5. Choisir le mode de paiement
6. Recevoir une confirmation de commande par email

🚚 LIVRAISON:
- Zones de livraison: Principales villes du Maroc (Casablanca, Rabat, Marrakech, Fès, Tanger, etc.)
- Délai de livraison: 2-5 jours ouvrables selon la zone
- Frais de livraison: Variables selon la zone et le poids
- Suivi de commande: Disponible dans 'Mes Commandes'

💳 PAIEMENT:
- Paiement à la livraison (Cash on Delivery)
- Carte bancaire (sécurisé)
- Virement bancaire

👤 COMPTE CLIENT:
- Créer un compte pour suivre les commandes
- Gérer les adresses de livraison
- Consulter l'historique des achats
- Ajouter des produits aux favoris
- Gérer le profil personnel

⭐ FONCTIONNALITÉS:
- Recherche de produits par nom ou catégorie
- Filtres par prix, coopérative, région
- Système de favoris pour sauvegarder des produits
- Avis et évaluations des produits
- Suggestions de produits similaires

📞 SUPPORT CLIENT:
- Email: support@Cooporia.ma
- Téléphone: +212 XXX-XXXXXX
- Horaires: Lun-Ven 9h-18h

🔒 SÉCURITÉ:
- Paiements sécurisés
- Protection des données personnelles
- Transactions cryptées

ℹ️ POLITIQUES:
- Retours acceptés sous 14 jours (produits non alimentaires)
- Remboursement ou échange selon le cas
- Produits garantis authentiques et de qualité

🎯 TA MISSION:
- Aider les utilisateurs à trouver des produits
- Expliquer le processus de commande et livraison
- Répondre aux questions sur les coopératives et produits
- Guider dans la navigation du site
- Être courtois, professionnel et utile
- Répondre TOUJOURS en français
- Si tu ne connais pas une information précise (prix exact, stock), suggère de consulter la page produit ou contacter le support

Question de l'utilisateur: " + userMessage;
                
                if (!string.IsNullOrEmpty(context))
                {
                    systemPrompt += "\n\nContexte supplémentaire: " + context;
                }
                // Construire la requête JSON
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = systemPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500,
                        topP = 0.8,
                        topK = 40
                    }
                };
                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                // Appeler l'API Gemini
                string url = $"{_apiUrl}?key={_apiKey}";
                
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject result = JObject.Parse(jsonResponse);
                    
                    string botResponse = result["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    if (string.IsNullOrEmpty(botResponse))
                    {
                        return "Erreur: Réponse vide de l'API Gemini. JSON: " + jsonResponse.Substring(0, Math.Min(200, jsonResponse.Length));
                    }
                    
                    return botResponse;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return $"Erreur API {response.StatusCode}: {errorContent.Substring(0, Math.Min(200, errorContent.Length))}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                return "Erreur réseau: " + httpEx.Message + " - Vérifiez votre connexion internet";
            }
            catch (TaskCanceledException)
            {
                return "Erreur: Timeout - L'API Gemini ne répond pas";
            }
            catch (Exception ex)
            {
                return "Erreur: " + ex.GetType().Name + " - " + ex.Message;
            }
        }
    }
}