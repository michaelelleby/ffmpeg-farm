using System;
using System.Collections.Generic;
using System.Linq;
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
                videoSb.Append(Video(encodingjob.VideoParam));

                if (encodingjob.VideoParam.Size != null)
                {
                    filterList.Add(Resize(encodingjob.VideoParam.Size));
                }
            }

            if (filterList.Count > 0)
            {
                sb.Append($@"-filter_complex ""{string.Join(";", filterList)}"" ");
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

        private static string Resize(VideoSize size)
        {
            if (size == null) throw new ArgumentNullException(nameof(size));

            return $@"scale={size.Width}:{size.Height}";
        }

        private static string Video(FFmpegParameters.Video parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            string preset = string.IsNullOrWhiteSpace(parameters.Preset) ? "medium" : parameters.Preset;

            return $@" -codec:v {parameters.Codec.ToString().ToLower()} -preset {preset} -b:v {parameters.Bitrate / 1024}k";
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
