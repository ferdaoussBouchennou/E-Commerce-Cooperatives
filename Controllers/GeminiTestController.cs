using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace E_Commerce_Cooperatives.Controllers
{
    public class GeminiTestController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> TestApi(string message)
        {
            try
            {
                var apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
                var apiUrl = ConfigurationManager.AppSettings["GeminiApiUrl"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return Json(new { success = false, error = "Clé API non configurée" });
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(60);

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = message }
                                }
                            }
                        }
                    };

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"{apiUrl}?key={apiKey}";

                    var response = await client.PostAsync(url, content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    return Json(new
                    {
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        response = responseText,
                        url = apiUrl,
                        keyLength = apiKey.Length,
                        keyStart = apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "..."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.GetType().Name,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
