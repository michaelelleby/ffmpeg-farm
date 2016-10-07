using System.Collections.Generic;
using Contract.Models;

namespace Contract
{
    public class JobStatusModel
    {
        public IEnumerable<FfmpegJobModel> Requests { get; set; }
    }
}