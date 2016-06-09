using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Dapper;

namespace ffmpeg_farm_server.Controllers
{
    public class StatusController : ApiController
    {
        public JobResult GetStatus()
        {
            IEnumerable<dynamic> jobs;
            IEnumerable<JobResultModel> requests;
            using (var connection = Helper.GetConnection())
            {
                requests = connection.Query<JobResultModel>("SELECT * from FfmpegRequest").ToList();
                jobs = connection.Query("SELECT * FROM FfmpegJobs").ToList();
            }

            foreach (dynamic request in requests)
            {
                request.Jobs = jobs.Where(x => x.JobCorrelationId == request.JobCorrelationId);
            }

            return new JobResult
            {
                Requests = requests
            };
        }
    }
}
