using System.Configuration;
using System.Data.SQLite;

namespace ffmpeg_farm_server.Controllers
{
    public class Helper
    {
        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString);
        }
    }
}