using System.Web.Mvc;

namespace E_Commerce_Cooperatives.Controllers
{
    public class ChatbotTestController : Controller
    {
        [HttpPost]
        public JsonResult TestMessage(string message)
        {
            return Json(new { 
                success = true, 
                response = "Test réussi! Vous avez envoyé: " + message 
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
