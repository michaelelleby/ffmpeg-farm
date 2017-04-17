using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace FFmpegFarm.Worker.Client
{
    public static class ClientHelper
    {
        public static readonly object Locker = new object();
    }
    
    public partial class TaskClient
    {
        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
        {
            #if DEBUGAPI
            lock (ClientHelper.Locker)
            {
                Console.WriteLine($"> {url}");
            }
            #endif
        }
    }

    public partial class StatusClient
    {
        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
        {
            #if DEBUGAPI
            lock (ClientHelper.Locker)
            {
                Console.WriteLine($"> {url}");
            }
            #endif 
        }
    }
}
