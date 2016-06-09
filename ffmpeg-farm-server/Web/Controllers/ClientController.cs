using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using Contract;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class ClientController : Controller
    {
        // GET: Client
        public ActionResult Index()
        {
            IEnumerable<ClientHeartbeat> heartbeats = null;
            using (var client = new HttpClient())
            {
                heartbeats = JsonConvert.DeserializeObject<IEnumerable<ClientHeartbeat>>(
                    client.GetStringAsync(ConfigurationManager.AppSettings["ApiUrl"] + "/api/client").Result);
            }

            return View(heartbeats);
        }
    }
}