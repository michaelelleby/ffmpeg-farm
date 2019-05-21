using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using DR.Common.Monitoring.Contract;
using DR.Common.Monitoring.Web.Models;

namespace API.WindowsService.Controllers
{
    public class SystemStatusController : ApiController
    {
        private const string ApplicationName = "FfmpegFarm";
        private readonly ISystemStatus _systemStatus;
        private readonly bool _rewriteScomWarningsToOk;

        public SystemStatusController(ISystemStatus systemStatus)
        {
            _systemStatus = systemStatus;
            if (!bool.TryParse(ConfigurationManager.AppSettings["RewriteScomWarningsToOk"], out var scomHack))
            {
                scomHack = false;
            }
            _rewriteScomWarningsToOk = scomHack;
        }

        /// <summary>
        /// SCOM endpoint.
        /// </summary>
        /// <returns>SCOM xml. This is the format they want.</returns>
        [HttpGet]
        [Route(nameof(Index))]
        public HttpResponseMessage Index()
        {
            var statuses = _systemStatus.RunAllChecks().ToArray();

            var res = new Monitoring(statuses, DateTime.Now, "DR.FfmpegFarm.WindowsService.Controllers.Api");
            if (_rewriteScomWarningsToOk)
            {
                res.HideWarnings();
            }

            var xs = new System.Xml.Serialization.XmlSerializer(res.GetType());
            using (var tw = new StringWriter())
            {
                xs.Serialize(tw, res);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(tw.ToString(), System.Text.Encoding.UTF8, "application/xml")
                };
            }
        }

        [HttpGet]
        [Route(nameof(Heartbeat))]
        public IHttpActionResult Heartbeat()
        {
            return Ok();
        }

        /// <summary>
        /// Ace probe end point. Doesn't check anything at the moment other when the API is running. 
        /// </summary>
        /// <returns>Status string.</returns>
        [HttpGet]
        [Route(nameof(GetAceProbe))]
        public string GetAceProbe()
        {
            return $"{ApplicationName} is running ok on {Environment.MachineName}. Time : {DateTime.Now.ToLocalTime()}";
        }

        /// <summary>
        /// Get health check for a specific check
        /// </summary>
        /// <param name="id">id of check to perform</param>
        /// <returns>requested health check status</returns>
        [HttpGet]
        [Route(nameof(Get) + "/{id}")]
        public SystemStatusModel.Check Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (!_systemStatus.Names.Contains(id))
            {
                throw new KeyNotFoundException("no check with id " + id);
            }

            var status = _systemStatus.RunCheck(id);
            return new SystemStatusModel.Check(status);
        }

        /// <summary>
        /// Run every registered health check
        /// </summary>
        /// <returns>List of health checks</returns>
        [HttpGet]
        [Route(nameof(GetAll))]
        public IEnumerable<SystemStatusModel.Check> GetAll()
        {
            var status = _systemStatus.RunAllChecks();
            return status.Select(c => new SystemStatusModel.Check(c));
        }
    }
}