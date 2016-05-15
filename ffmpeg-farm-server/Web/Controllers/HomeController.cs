using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Web.Mvc;
using Contract;
using Dapper;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            IDictionary<TranscodingJob, IEnumerable<FfmpegPart>> model = new ConcurrentDictionary<TranscodingJob, IEnumerable<FfmpegPart>>();

            using (var connection = GetConnection())
            {
                connection.Open();

                var jobs = connection.Query<TranscodingJob>("SELECT Id, Arguments, SourceFilename, JobCorrelationId FROM FfmpegJobs;");
                foreach (TranscodingJob job in jobs)
                {
                    //var parts = connection.Query<FfmpegPart>(
                    //    "SELECT Filename, Number, Target, (SELECT SourceFilename FROM FfmpegJobs WHERE JobCorrelationId = FfmpegParts.JobCorrelationId) AS SourceFilename, JobCorrelationId FROM FfmpegParts WHERE JobCorrelationId = ?",
                    //    job.JobCorrelationId);

                    model.Add(job, null);
                }
            }

            return View(model);
        }

        private static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString);
        }
    }
}