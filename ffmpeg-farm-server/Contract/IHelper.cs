using System.Data;

namespace Contract
{
    public interface IHelper
    {
        IDbConnection GetConnection();
        Mediainfo GetMediainfo(string sourceFilename);
        int GetDuration(string sourceFilename);
        void InsertClientHeartbeat(string machineName);
    }
}