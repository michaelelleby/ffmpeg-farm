using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class ScrubbingJobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public ScrubbingJobController(IHelper helper, ILogging logging)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _helper = helper;
            _logging = logging;
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public Guid CreateNew(ScrubbingJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewScrubbingJob(input);
            _logging.Info($"Created new scrubbing job : {res}");
            return res;
        }

        private Guid HandleNewScrubbingJob(ScrubbingJobRequestModel request)
        {
            return Guid.Empty;
        }
    }
}
