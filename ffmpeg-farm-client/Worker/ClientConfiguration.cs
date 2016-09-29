using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FFmpegFarm.Worker.Client
{
    public partial class AudioJobClient
    {
        partial void PrepareRequest(HttpClient request, ref string url)
        {
            request.Timeout = TimeSpan.FromSeconds(4);
        }
    }
}
