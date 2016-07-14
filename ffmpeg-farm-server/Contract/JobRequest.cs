using System;
using Newtonsoft.Json;

namespace Contract
{
    public class JobRequest
    {
        /// <summary>
        /// Video source
        /// </summary>
        public string VideoSourceFilename { get; set; }

        /// <summary>
        /// Source for audio. This is optional and if specified, the audio from the video file
        /// will be discarded and this will be the audio track in the output instead
        /// </summary>
        public string AudioSourceFilename { get; set; }

        /// <summary>
        /// Output filename
        /// </summary>
        public string DestinationFilename { get; set; }

        /// <summary>
        /// Latest timestamp when the output files are needed
        /// This will be used to prioritize requests when there
        /// is a queue
        /// </summary>
        public DateTime Needed { get; set; }

        /// <summary>
        ///  One or more target resolutions and bitrates
        /// </summary>
        public DestinationFormat[] Targets { get; set; }

        /// <summary>
        /// Preset for X264. One of ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow, placebo.
        /// See http://dev.beandog.org/x264_preset_reference.html for description
        /// </summary>
        public string X264Preset { get; set; }

        /// <summary>
        /// Whether the output should support MPEG DASH
        /// </summary>
        public bool EnableDash { get; set; }

        [JsonIgnore]
        public bool HasAlternateAudio => !string.IsNullOrWhiteSpace(AudioSourceFilename) && !string.IsNullOrWhiteSpace(VideoSourceFilename);

        [JsonIgnore]
        public Guid JobCorrelationId { get; set; }
    }
}