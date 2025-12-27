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
                return Json(new { success = false, error = "Message vide" }, JsonRequestBehavior.AllowGet);
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

                return Json(new { success = true, response = response }, JsonRequestBehavior.AllowGet);
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, error = "Erreur serveur: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
