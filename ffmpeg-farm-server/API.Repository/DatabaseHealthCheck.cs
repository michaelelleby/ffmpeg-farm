using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Contract;
using DR.Common.Monitoring.Models;

namespace API.Repository
{
    public class DatabaseHealthCheck : CommonHealthCheck
    {
        private readonly IJobRepository _jobRepository;
        private readonly string _connectionString;

        public DatabaseHealthCheck(IJobRepository jobRepository, string connectionString) :
            base($"OD3.FfmpegFarm.{nameof(DatabaseHealthCheck)}", descriptionText: "Database checker")
        {
            _jobRepository = jobRepository;
            _connectionString = connectionString;
        }

        protected override void RunTest(StatusBuilder statusBuilder)
        {
            statusBuilder.MessageBuilder.Append($"Testing database {GetSqlDbDescription(_connectionString)}. ");
            var latestJob = _jobRepository.Get(1).FirstOrDefault();
            if (latestJob == null)
            {
                statusBuilder.CurrentLevel = SeverityLevel.Warning;
                statusBuilder.Passed = false;
                statusBuilder.MessageBuilder.AppendLine("No jobs found in the database");
            }
            else
            {
                statusBuilder.Passed = true;
                statusBuilder.MessageBuilder.AppendLine(
                    $"DB ok, latest job {latestJob.JobCorrelationId}, from {latestJob.Created}");
            }

        }

        private static string GetSqlDbDescription(string dsn)
        {
            if (string.IsNullOrWhiteSpace(dsn))
                throw new ConfigurationErrorsException("No ConnectionString was set for mssql database.");

            var builder = new SqlConnectionStringBuilder(dsn);

            return $"Server name: {builder.DataSource}. Database name: {builder.InitialCatalog}";
        }

        protected override void HandleException(Exception ex, StatusBuilder builder)
        {
            builder.MessageBuilder.Append($"Failed, check db and network connection to db. Exception: {ex.Message}");
        }
    }
}
