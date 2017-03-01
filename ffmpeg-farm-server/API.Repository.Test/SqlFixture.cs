using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace API.Repository.Test
{
    public class SqlFixture : IDisposable
    {
        private readonly SqlConnectionStringBuilder _connectionStringBuilder;
        public string ConnectionString { get; private set; }

        public SqlFixture()
        {
            _connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "OD01UDV",
                InitialCatalog = "FFmpegFarmIntegrationTests" + Guid.NewGuid().ToString("N"),
                IntegratedSecurity = false,
                UserID = "nunit",
                Password = "test",
                Authentication = SqlAuthenticationMethod.SqlPassword,
                TrustServerCertificate = true,
                Pooling = true
            };

            ConnectionString = _connectionStringBuilder.ConnectionString;
            Console.WriteLine("SqlFixture created for " + ConnectionString);
            CreateTestDatabase();
        }

        private void ExecuteNonQuery(string sqlStatement)
        {
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(ConnectionString);
            string databaseName = scsb.InitialCatalog;
            scsb.InitialCatalog = "master";

            using (SqlConnection conn = new SqlConnection(scsb.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(string.Format(CultureInfo.InvariantCulture, sqlStatement, databaseName), conn))
                {
                    Console.WriteLine("con > " + conn.ConnectionString);
                    Console.WriteLine("cmd > " + cmd.CommandText);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateTestDatabase()
        {
            ExecuteNonQuery(
@"USE MASTER;
CREATE DATABASE [{0}]");
            ExecuteNonQuery(
@"USE MASTER;
ALTER DATABASE [{0}] SET ALLOW_SNAPSHOT_ISOLATION ON");

            // Tables for Ondemand database
            ExecuteScript(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "SchemaCreate.sql")));
        }

        private void DeleteTestDatabase()
        {
            string deleteSql = string.Format(@"
USE MASTER;
IF EXISTS (SELECT [name] FROM sys.databases WHERE [name] = '{0}')
BEGIN
  ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [{0}];
END", _connectionStringBuilder.InitialCatalog);
            ExecuteNonQuery(deleteSql);
        }

        private void ExecuteScript(string sqlScript)
        {
            using (SqlCommand sqlcmd = new SqlCommand())
            {
                using (sqlcmd.Connection = new SqlConnection(ConnectionString))
                {
                    sqlcmd.Connection.Open();

                    sqlScript = sqlScript.Replace("[ffmpegfarm]", string.Format("[{0}]", _connectionStringBuilder.InitialCatalog));

                    foreach (string command in sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // Remove all "USE [database]" commands from the script
                        sqlcmd.CommandText = Regex.Replace(command, @"^USE.+$", string.Empty,
                            RegexOptions.IgnorePatternWhitespace |
                            RegexOptions.Multiline);
                        sqlcmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Dispose()
        {
            try
            {
                DeleteTestDatabase();
            }
            catch
            {
                // ignored
            }
        }

        public void Reset()
        {
            var dropTestData = @"
USE [{0}];
";
            ExecuteNonQuery(dropTestData);
        }
    }
}