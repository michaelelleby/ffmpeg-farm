using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Web.Mvc;
using Contract;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            dynamic requests;
            using (HttpClient client = new HttpClient())
            {
                string result = client.GetStringAsync(ConfigurationManager.AppSettings["ApiUrl"] + "/api/status").Result;
                requests = JsonConvert.DeserializeObject<JobResult>(result);
            }

            return View(requests);
        }

        public class JobResult
        {
            [JsonProperty(PropertyName = "Requests")]
            public ICollection<JobResultModel> Jobs { get; set; }
        }
    }
}