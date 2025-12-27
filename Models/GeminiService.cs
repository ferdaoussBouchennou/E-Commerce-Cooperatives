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
            _apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            _apiUrl = ConfigurationManager.AppSettings["GeminiApiUrl"];
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

                // Construire le prompt avec le contexte de votre site
                string systemPrompt = @"Tu es un assistant virtuel pour CoopShop, une plateforme e-commerce de coopératives.
                
Informations sur le site:
- CoopShop est une plateforme qui vend des produits de coopératives locales
- Les clients peuvent parcourir des produits par catégories
- Les clients peuvent ajouter des produits au panier et passer commande
- Livraison disponible dans différentes zones
- Support client disponible pour toute question
Ta mission:
- Aider les utilisateurs à naviguer sur le site
- Répondre aux questions sur les produits, commandes, livraison
- Être courtois, professionnel et utile
- Répondre en français
- Si tu ne connais pas la réponse, suggère de contacter le support
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