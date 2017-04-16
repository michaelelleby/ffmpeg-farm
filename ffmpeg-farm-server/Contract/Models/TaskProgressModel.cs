using System;
using Microsoft.Build.Framework;

namespace Contract.Models
{
    public class TaskProgressModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTimeOffset Timestamp { get; set; }

        [Required]
        public string MachineName { get; set; }

        [Required]
        public bool Failed { get; set; }

        [Required]
        public bool Done { get; set; }

        [Required]
        public TimeSpan Progress { get; set; }

        public TimeSpan? VerifyProgress { get; set; }
    }
}