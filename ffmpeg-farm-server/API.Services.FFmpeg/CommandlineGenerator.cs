using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Contract;

namespace API.Services.FFmpeg
{
    public class CommandlineGenerator
    {
        public static string Get(FFmpegParameters encodingjob)
        {
            var sb = new StringBuilder($@"-i ""{encodingjob.Inputfile}"" ");
            var filterList = new List<string>();
            var audioSb = new StringBuilder();
            var videoSb = new StringBuilder();

            if (encodingjob.Deinterlace != null)
            {
                filterList.Add(Deinterlace(encodingjob.Deinterlace));
            }

            if (encodingjob.AudioParam != null)
            {
                audioSb.Append(Audio(encodingjob.AudioParam));
            }

            if (encodingjob.VideoParam != null)
            {
                if (encodingjob.VideoParam.VideoTarget.Count > 1)
                {
                    int mapIndex = 0;
                    var resolutions = encodingjob.VideoParam.VideoTarget.GroupBy(t => t.Size);
                    foreach (var resolution in resolutions)
                    {
                        filterList.Add(Resize(resolution.Key));
                        filterList.Add(Split(resolution.ToList()));

                        foreach (VideoTarget target in resolution)
                        {
                            videoSb.Append(VideoMap(target, VideoCodec.LibX264, $"[out{mapIndex++}]"));
                        }
                    }
                }
                else
                {
                    FFmpegParameters.Video parameters = encodingjob.VideoParam;
                    VideoTarget target = parameters.VideoTarget.Single();
                    videoSb.Append(Video(target, parameters.Codec));

                    if (target.Size != null)
                    {
                        filterList.Add(Resize(target.Size));
                    }
                }
            }

            if (filterList.Count > 0)
            {
                sb.Append($@"-filter_complex ""{string.Join(",", filterList)}"" ");
            }

            if (videoSb.Length > 0)
            {
                sb.Append(videoSb);
            }

            if (audioSb.Length > 0)
            {
                sb.Append(audioSb);
            }

            return Regex.Replace(sb.ToString().Trim(), @"\s{2,}", " ");
        }

        private static string VideoMap(VideoTarget target, VideoCodec codec, string map)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            string preset = string.IsNullOrWhiteSpace(target.Preset) ? "medium" : target.Preset;

            return $@" -map {map} -codec:v {codec.ToString().ToLower()} -preset {preset} -b:v {target.Bitrate / 1024}k";
        }

        private static string Split(ICollection<VideoTarget> resolution)
        {
            if (resolution == null) throw new ArgumentNullException(nameof(resolution));

            StringBuilder sb = new StringBuilder($"split={resolution.Count}");
            for (int i = 0; i < resolution.Count; i++)
            {
                sb.Append($"[out{i}]");
            }

            return sb.ToString();
        }

        private static string Resize(VideoSize size)
        {
            if (size == null) throw new ArgumentNullException(nameof(size));

            return $@"scale={size.Width}:{size.Height}";
        }

        private static string Video(VideoTarget target, VideoCodec codec)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (codec == VideoCodec.Unknown) throw new ArgumentOutOfRangeException(nameof(codec));
            
            string preset = string.IsNullOrWhiteSpace(target.Preset) ? "medium" : target.Preset;

            return $@" -codec:v {codec.ToString().ToLower()} -preset {preset} -b:v {target.Bitrate / 1024}k";
        }

        private static string Deinterlace(FFmpegParameters.DeinterlaceSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return $@"yadif={(int) settings.Mode}:{(int) settings.Parity}:{(settings.DeinterlaceAllFrames ? 0 : 1)}";
        }

        private static string Audio(FFmpegParameters.Audio audio)
        {
            if (audio == null) throw new ArgumentNullException(nameof(audio));

            return $@" -codec:a {audio.Codec.ToString().ToLower()} -b:a {audio.Bitrate / 1024}k ";
        }
    }
}
