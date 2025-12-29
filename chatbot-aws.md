Guide Complet: Chatbot Gemini + Déploiement AWS
Guide complet pour ajouter un chatbot Gemini gratuit à votre application E-Commerce-Cooperatives et la déployer sur AWS Elastic Beanstalk.

📋 Vue d'ensemble du Projet
Votre application est un site e-commerce ASP.NET MVC (.NET Framework 4.7.2) avec:

Backend: ASP.NET MVC 5, Entity Framework, SQL Server
Frontend: Razor Views, Bootstrap, jQuery
Architecture: Modèle MVC classique
🤖 PARTIE 1: Intégration du Chatbot Gemini
Étape 1: Obtenir une Clé API Gemini (Gratuite)
Accéder à Google AI Studio

Visitez: https://makersuite.google.com/app/apikey
Connectez-vous avec votre compte Google
Créer une Clé API

Cliquez sur "Create API Key"
Sélectionnez "Create API key in new project" ou utilisez un projet existant
Copiez votre clé API (format: AIza...)
IMPORTANT

La clé API Gemini gratuite offre:

60 requêtes par minute
1500 requêtes par jour
Parfait pour un site avec trafic modéré
Étape 2: Installer les Packages NuGet Nécessaires
Ouvrez la Console du Gestionnaire de Packages dans Visual Studio:

# Package pour faire des appels HTTP à l'API Gemini
Install-Package Newtonsoft.Json
# Package System.Net.Http (normalement déjà installé)
# Si nécessaire:
Install-Package System.Net.Http
Étape 3: Configurer la Clé API
[MODIFY] 
Web.config
Ajoutez votre clé API dans la section <appSettings>:

<appSettings>
  <!-- Existing settings... -->
  
  <!-- Gemini AI Configuration -->
  <add key="GeminiApiKey" value="VOTRE_CLE_API_ICI" />
  <add key="GeminiApiUrl" value="https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent" />
</appSettings>
Étape 4: Créer le Service Gemini
[NEW] 
Models/GeminiService.cs
Service pour communiquer avec l'API Gemini:

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
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        public async Task<string> GetChatbotResponse(string userMessage, string context = "")
        {
            try
            {
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
                    
                    return botResponse ?? "Désolé, je n'ai pas pu générer une réponse.";
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return $"Erreur: {response.StatusCode}. Veuillez réessayer.";
                }
            }
            catch (Exception ex)
            {
                return "Désolé, une erreur s'est produite. Veuillez réessayer plus tard.";
            }
        }
    }
}
Étape 5: Créer le Contrôleur Chatbot
[NEW] 
Controllers/ChatbotController.cs
using System.Threading.Tasks;
using System.Web.Mvc;
using E_Commerce_Cooperatives.Models;
namespace E_Commerce_Cooperatives.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiService _geminiService;
        public ChatbotController()
        {
            _geminiService = new GeminiService();
        }
        [HttpPost]
        public async Task<JsonResult> SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, error = "Message vide" });
            }
            try
            {
                // Optionnel: Ajouter du contexte basé sur l'utilisateur connecté
                string context = "";
                if (Session["UserId"] != null)
                {
                    context = "L'utilisateur est connecté.";
                }
                string response = await _geminiService.GetChatbotResponse(message, context);
                return Json(new { success = true, response = response });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, error = "Erreur serveur" });
            }
        }
    }
}
Étape 6: Créer l'Interface Utilisateur du Chatbot
[NEW] 
Views/Shared/_Chatbot.cshtml
Widget de chatbot flottant:

<!-- Chatbot Widget -->
<div id="chatbot-container">
    <!-- Bouton flottant -->
    <button id="chatbot-toggle" class="chatbot-toggle" aria-label="Ouvrir le chatbot">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
        </svg>
    </button>
    <!-- Fenêtre de chat -->
    <div id="chatbot-window" class="chatbot-window" style="display: none;">
        <!-- Header -->
        <div class="chatbot-header">
            <div class="chatbot-header-content">
                <div class="chatbot-avatar">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"/>
                    </svg>
                </div>
                <div>
                    <h3 class="chatbot-title">Assistant CoopShop</h3>
                    <p class="chatbot-status">En ligne</p>
                </div>
            </div>
            <button id="chatbot-close" class="chatbot-close-btn" aria-label="Fermer">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="18" y1="6" x2="6" y2="18"></line>
                    <line x1="6" y1="6" x2="18" y2="18"></line>
                </svg>
            </button>
        </div>
        <!-- Messages -->
        <div id="chatbot-messages" class="chatbot-messages">
            <div class="chatbot-message bot-message">
                <div class="message-avatar">🤖</div>
                <div class="message-content">
                    <p>Bonjour! Je suis l'assistant virtuel de CoopShop. Comment puis-je vous aider aujourd'hui?</p>
                </div>
            </div>
        </div>
        <!-- Input -->
        <div class="chatbot-input-container">
            <input type="text" id="chatbot-input" class="chatbot-input" placeholder="Tapez votre message..." />
            <button id="chatbot-send" class="chatbot-send-btn" aria-label="Envoyer">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="22" y1="2" x2="11" y2="13"></line>
                    <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
                </svg>
            </button>
        </div>
    </div>
</div>
<style>
    /* Chatbot Styles */
    #chatbot-container {
        position: fixed;
        bottom: 20px;
        right: 20px;
        z-index: 9999;
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    }
    .chatbot-toggle {
        width: 60px;
        height: 60px;
        border-radius: 50%;
        background: linear-gradient(135deg, #305C7D 0%, #1e3d54 100%);
        border: none;
        color: white;
        cursor: pointer;
        box-shadow: 0 4px 12px rgba(48, 92, 125, 0.4);
        transition: all 0.3s ease;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .chatbot-toggle:hover {
        transform: scale(1.1);
        box-shadow: 0 6px 20px rgba(48, 92, 125, 0.6);
    }
    .chatbot-window {
        position: absolute;
        bottom: 80px;
        right: 0;
        width: 380px;
        height: 550px;
        background: white;
        border-radius: 16px;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.15);
        display: flex;
        flex-direction: column;
        overflow: hidden;
        animation: slideUp 0.3s ease;
    }
    @keyframes slideUp {
        from {
            opacity: 0;
            transform: translateY(20px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    .chatbot-header {
        background: linear-gradient(135deg, #305C7D 0%, #1e3d54 100%);
        color: white;
        padding: 16px;
        display: flex;
        justify-content: space-between;
        align-items: center;
    }
    .chatbot-header-content {
        display: flex;
        align-items: center;
        gap: 12px;
    }
    .chatbot-avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        background: rgba(255, 255, 255, 0.2);
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .chatbot-title {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
    }
    .chatbot-status {
        margin: 0;
        font-size: 12px;
        opacity: 0.9;
    }
    .chatbot-close-btn {
        background: none;
        border: none;
        color: white;
        cursor: pointer;
        padding: 4px;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 4px;
        transition: background 0.2s;
    }
    .chatbot-close-btn:hover {
        background: rgba(255, 255, 255, 0.1);
    }
    .chatbot-messages {
        flex: 1;
        overflow-y: auto;
        padding: 16px;
        background: #f8f9fa;
    }
    .chatbot-message {
        display: flex;
        gap: 8px;
        margin-bottom: 16px;
        animation: fadeIn 0.3s ease;
    }
    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(10px); }
        to { opacity: 1; transform: translateY(0); }
    }
    .message-avatar {
        width: 32px;
        height: 32px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 18px;
        flex-shrink: 0;
    }
    .bot-message .message-content {
        background: white;
        padding: 12px 16px;
        border-radius: 12px 12px 12px 4px;
        max-width: 75%;
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
    }
    .user-message {
        flex-direction: row-reverse;
    }
    .user-message .message-content {
        background: #305C7D;
        color: white;
        padding: 12px 16px;
        border-radius: 12px 12px 4px 12px;
        max-width: 75%;
    }
    .message-content p {
        margin: 0;
        font-size: 14px;
        line-height: 1.5;
    }
    .chatbot-input-container {
        padding: 16px;
        background: white;
        border-top: 1px solid #e9ecef;
        display: flex;
        gap: 8px;
    }
    .chatbot-input {
        flex: 1;
        padding: 12px 16px;
        border: 1px solid #dee2e6;
        border-radius: 24px;
        font-size: 14px;
        outline: none;
        transition: border-color 0.2s;
    }
    .chatbot-input:focus {
        border-color: #305C7D;
    }
    .chatbot-send-btn {
        width: 44px;
        height: 44px;
        border-radius: 50%;
        background: #305C7D;
        border: none;
        color: white;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.2s;
    }
    .chatbot-send-btn:hover {
        background: #1e3d54;
        transform: scale(1.05);
    }
    .chatbot-send-btn:disabled {
        background: #ccc;
        cursor: not-allowed;
        transform: none;
    }
    .typing-indicator {
        display: flex;
        gap: 4px;
        padding: 12px 16px;
    }
    .typing-dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #999;
        animation: typing 1.4s infinite;
    }
    .typing-dot:nth-child(2) { animation-delay: 0.2s; }
    .typing-dot:nth-child(3) { animation-delay: 0.4s; }
    @keyframes typing {
        0%, 60%, 100% { transform: translateY(0); }
        30% { transform: translateY(-10px); }
    }
    @media (max-width: 480px) {
        .chatbot-window {
            width: calc(100vw - 40px);
            height: calc(100vh - 100px);
        }
    }
</style>
<script>
    (function() {
        const toggle = document.getElementById('chatbot-toggle');
        const window = document.getElementById('chatbot-window');
        const close = document.getElementById('chatbot-close');
        const input = document.getElementById('chatbot-input');
        const sendBtn = document.getElementById('chatbot-send');
        const messagesContainer = document.getElementById('chatbot-messages');
        // Toggle chatbot
        toggle.addEventListener('click', function() {
            window.style.display = window.style.display === 'none' ? 'flex' : 'none';
            if (window.style.display === 'flex') {
                input.focus();
            }
        });
        close.addEventListener('click', function() {
            window.style.display = 'none';
        });
        // Send message
        function sendMessage() {
            const message = input.value.trim();
            if (!message) return;
            // Add user message
            addMessage(message, 'user');
            input.value = '';
            sendBtn.disabled = true;
            // Show typing indicator
            const typingIndicator = addTypingIndicator();
            // Send to server
            fetch('@Url.Action("SendMessage", "Chatbot")', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message: message })
            })
            .then(response => response.json())
            .then(data => {
                typingIndicator.remove();
                if (data.success) {
                    addMessage(data.response, 'bot');
                } else {
                    addMessage('Désolé, une erreur s\'est produite. Veuillez réessayer.', 'bot');
                }
                sendBtn.disabled = false;
            })
            .catch(error => {
                typingIndicator.remove();
                addMessage('Erreur de connexion. Veuillez vérifier votre connexion internet.', 'bot');
                sendBtn.disabled = false;
            });
        }
        sendBtn.addEventListener('click', sendMessage);
        input.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                sendMessage();
            }
        });
        function addMessage(text, type) {
            const messageDiv = document.createElement('div');
            messageDiv.className = `chatbot-message ${type}-message`;
            
            const avatar = document.createElement('div');
            avatar.className = 'message-avatar';
            avatar.textContent = type === 'bot' ? '🤖' : '👤';
            
            const content = document.createElement('div');
            content.className = 'message-content';
            
            const p = document.createElement('p');
            p.textContent = text;
            
            content.appendChild(p);
            messageDiv.appendChild(avatar);
            messageDiv.appendChild(content);
            
            messagesContainer.appendChild(messageDiv);
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
        function addTypingIndicator() {
            const messageDiv = document.createElement('div');
            messageDiv.className = 'chatbot-message bot-message';
            
            const avatar = document.createElement('div');
            avatar.className = 'message-avatar';
            avatar.textContent = '🤖';
            
            const content = document.createElement('div');
            content.className = 'message-content';
            
            const typing = document.createElement('div');
            typing.className = 'typing-indicator';
            typing.innerHTML = '<div class="typing-dot"></div><div class="typing-dot"></div><div class="typing-dot"></div>';
            
            content.appendChild(typing);
            messageDiv.appendChild(avatar);
            messageDiv.appendChild(content);
            
            messagesContainer.appendChild(messageDiv);
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
            
            return messageDiv;
        }
    })();
</script>
Étape 7: Intégrer le Chatbot dans le Layout
[MODIFY] 
Views/Shared/_Layout.cshtml
Ajoutez le chatbot avant la balise </body>:

<body>
    <div id="user-authenticated" data-authenticated="@((Session["UserId"] != null).ToString().ToLower())" style="display: none;"></div>
    @RenderBody()
    
    <!-- Chatbot Widget -->
    @Html.Partial("_Chatbot")
    @Scripts.Render("~/bundles/jquery")
    @Scripts.Render("~/bundles/bootstrap")
    @Scripts.Render("~/bundles/site")
    @RenderSection("scripts", required: false)
</body>
Étape 8: Mettre à Jour le Projet
Ajoutez les nouveaux fichiers au projet 
.csproj
:

Clic droit sur le projet dans l'Explorateur de solutions
Ajouter > Classe existante
Sélectionnez GeminiService.cs et ChatbotController.cs
Pour la vue, clic droit sur Views/Shared > Ajouter > Vue existante


🚀 PARTIE 2: Déploiement sur AWS Elastic Beanstalk
Prérequis
Compte AWS (niveau gratuit disponible)
Visual Studio 2019/2022
AWS Toolkit for Visual Studio
Étape 1: Installer AWS Toolkit pour Visual Studio
Ouvrir Visual Studio
Extensions > Gérer les extensions
Rechercher "AWS Toolkit for Visual Studio"
Télécharger et installer
Redémarrer Visual Studio
Étape 2: Configurer AWS Credentials
Créer un utilisateur IAM dans AWS:

Connectez-vous à la Console AWS
Allez dans IAM > Users > Add user
Nom: elastic-beanstalk-deployer
Access type: Programmatic access
Permissions: AdministratorAccess-AWSElasticBeanstalk
Copiez Access Key ID et Secret Access Key
Dans Visual Studio:

View > AWS Explorer
Cliquez sur Add AWS Credentials
Entrez vos Access Key ID et Secret Access Key
Profile Name: default
Étape 3: Préparer l'Application pour le Déploiement
A. Configurer la Base de Données pour AWS RDS
WARNING

Pour AWS, vous devrez utiliser AWS RDS (SQL Server) au lieu de votre base de données locale.

[MODIFY] 
Web.config
Modifiez la chaîne de connexion pour utiliser des variables d'environnement:

<connectionStrings>
  <add name="ECommerceConnection" 
       connectionString="Data Source={RDS_HOSTNAME};Initial Catalog={RDS_DB_NAME};User ID={RDS_USERNAME};Password={RDS_PASSWORD};Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
B. Créer un fichier de transformation Web.Release.config
Le fichier existe déjà, mais vérifiez qu'il transforme correctement la connexion:

<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
  <connectionStrings>
    <add name="ECommerceConnection" 
         connectionString="Data Source={RDS_HOSTNAME};Initial Catalog={RDS_DB_NAME};User ID={RDS_USERNAME};Password={RDS_PASSWORD};" 
         xdt:Transform="SetAttributes" 
         xdt:Locator="Match(name)"/>
  </connectionStrings>
  
  <system.web>
    <compilation xdt:Transform="RemoveAttributes(debug)" />
  </system.web>
</configuration>
Étape 4: Créer une Base de Données RDS (Free Tier)
Console AWS > RDS > Create database

Configuration:

Engine: SQL Server Express (Free tier eligible)
Template: Free tier
DB instance identifier: ecommerce-db
Master username: admin
Master password: (choisissez un mot de passe fort)
DB instance class: db.t3.micro (Free tier)
Storage: 20 GB (Free tier)
Public access: Yes (pour la migration initiale)
VPC security group: Créer nouveau
Créer la base de données (prend 5-10 minutes)

Configurer le Security Group:

Allez dans EC2 > Security Groups
Trouvez le security group de votre RDS
Inbound rules > Edit
Ajoutez: Type: MSSQL, Port: 1433, Source: Anywhere (0.0.0.0/0)
Étape 5: Migrer la Base de Données vers RDS
Option A: Utiliser SQL Server Management Studio (SSMS)
Exporter la base locale:

-- Dans SSMS, clic droit sur votre base 'ecommerce'
-- Tasks > Generate Scripts
-- Sélectionnez tous les objets
-- Advanced > Types of data to script: Schema and data
Se connecter à RDS:

Server: [endpoint-rds].rds.amazonaws.com
Authentication: SQL Server Authentication
Login: admin
Password: votre mot de passe
Exécuter le script sur la base RDS

Option B: Utiliser le fichier ecommerce.sql existant
# Depuis votre machine locale
sqlcmd -S [endpoint-rds].rds.amazonaws.com -U admin -P [password] -i "c:\Users\HP\source\repos\E-Commerce-Cooperatives\ecommerce.sql"


Étape 6: Déployer sur Elastic Beanstalk via Visual Studio


Clic droit sur le projet dans Visual Studio

Publish to AWS Elastic Beanstalk...

Configuration du déploiement:

Application:

Application name: ecommerce-cooperatives
Environment name: ecommerce-cooperatives-prod
AWS Options:

Container type: 64bit Windows Server 2019 v2.x running IIS 10.0
Instance type: t2.micro (Free tier)
Key pair: Créer ou sélectionner une paire de clés
Application Options:

Build configuration: Release
Framework: .NET Framework 4.7.2
Environment Variables (Variables d'environnement):

Ajoutez les variables suivantes:

RDS_HOSTNAME = [votre-endpoint-rds].rds.amazonaws.com
RDS_DB_NAME = ecommerce
RDS_USERNAME = admin
RDS_PASSWORD = [votre-mot-de-passe]
GeminiApiKey = [votre-clé-gemini]
Permissions:

Service role: Créer nouveau ou utiliser aws-elasticbeanstalk-service-role
Instance profile: Créer nouveau ou utiliser aws-elasticbeanstalk-ec2-role
Cliquez sur Deploy

Étape 7: Configuration Post-Déploiement
A. Configurer le Health Check
Console AWS > Elastic Beanstalk > Votre environnement
Configuration > Load balancer
Health check path: / ou /Home/Index
B. Configurer HTTPS (Optionnel mais recommandé)
Obtenir un certificat SSL:

AWS Certificate Manager (gratuit)
Demander un certificat pour votre domaine
Configurer le Load Balancer:

Ajouter un listener HTTPS (port 443)
Attacher le certificat SSL
C. Configurer un Nom de Domaine (Optionnel)
Route 53 ou votre registrar de domaine
Créer un enregistrement CNAME pointant vers l'URL Elastic Beanstalk
Étape 8: Tester le Déploiement
URL de l'application: http://[environment-name].[region].elasticbeanstalk.com
Vérifier:
✅ Page d'accueil charge correctement
✅ Connexion à la base de données fonctionne
✅ Chatbot Gemini répond aux messages
✅ Toutes les fonctionnalités marchent
📊 Monitoring et Maintenance
Logs et Debugging
Dans Visual Studio:

AWS Explorer > Elastic Beanstalk > Votre environnement
View Logs
Console AWS:

Elastic Beanstalk > Logs > Request Logs
Mises à Jour
Pour déployer une nouvelle version:

Faire vos modifications dans Visual Studio
Clic droit > Publish to AWS Elastic Beanstalk
Sélectionner l'environnement existant
Deploy
💰 Estimation des Coûts (Free Tier)
Première année (Free Tier):

✅ EC2 t2.micro: 750 heures/mois (gratuit)
✅ RDS db.t3.micro: 750 heures/mois (gratuit)
✅ Elastic Beanstalk: Gratuit (vous payez seulement les ressources)
✅ Gemini API: 1500 requêtes/jour (gratuit)
✅ 5 GB de stockage RDS (gratuit)
Après la première année:

EC2 t2.micro: ~$8-10/mois
RDS db.t3.micro: ~$15-20/mois
Total: ~$25-30/mois
🔧 Dépannage
Problème: L'application ne démarre pas
Solution:

Vérifier les logs dans AWS Console
Vérifier que la configuration .NET Framework est correcte
S'assurer que tous les packages NuGet sont restaurés
Problème: Erreur de connexion à la base de données
Solution:

Vérifier les variables d'environnement RDS
Vérifier le Security Group RDS (port 1433 ouvert)
Tester la connexion avec SSMS
Problème: Le chatbot ne répond pas
Solution:

Vérifier la clé API Gemini dans les variables d'environnement
Vérifier les logs du contrôleur Chatbot
Tester l'API Gemini directement avec Postman
📚 Ressources Supplémentaires
Documentation AWS Elastic Beanstalk
Documentation Gemini API
AWS Free Tier
AWS Toolkit for Visual Studio
✅ Checklist de Déploiement
 Clé API Gemini obtenue et configurée
 Packages NuGet installés
 GeminiService.cs créé
 ChatbotController.cs créé
 Vue _Chatbot.cshtml créée
 Chatbot intégré dans _Layout.cshtml
 Chatbot testé localement
 AWS Toolkit installé
 Credentials AWS configurés
 Base de données RDS créée
 Données migrées vers RDS
 Variables d'environnement configurées
 Application déployée sur Elastic Beanstalk
 Tests post-déploiement effectués
 Monitoring configuré
TIP

Conseil: Commencez par tester le chatbot localement avant de déployer sur AWS. Cela vous permettra de déboguer plus facilement.

IMPORTANT

Sécurité: Ne commitez JAMAIS vos clés API dans Git. Utilisez toujours des variables d'environnement ou AWS Secrets Manager pour les données sensibles.