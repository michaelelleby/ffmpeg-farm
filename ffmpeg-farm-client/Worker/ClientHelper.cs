using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace FFmpegFarm.Worker.Client
{
    public static class ClientHelper
    {
        public static readonly object Locker = new object();
    }
}
