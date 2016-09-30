using System;
using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace FFmpegFarm.Worker.Client
{
    public static class ClientHelper
    {
        public static TimeSpan TimeOut => TimeSpan.FromSeconds(10);
    }
    
    public partial class AudioJobClient
    {
        partial void PrepareRequest(HttpClient request, ref string url)
        {
            request.Timeout = ClientHelper.TimeOut;
        }
    }

    public partial class StatusClient
    {
        partial void PrepareRequest(HttpClient request, ref string url)
        {
            request.Timeout = ClientHelper.TimeOut;
        }
    }
}
