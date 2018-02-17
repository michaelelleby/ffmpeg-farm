using System;
using Contract;

namespace Utils
{
    public static class ContainerHelper
    {
        public static string GetExtension(ContainerFormat containerFormat)
        {
            switch (containerFormat)
            {
                case ContainerFormat.AAC:
                    return "aac";
                    case ContainerFormat.MP3:
                    return "mp3";
                case ContainerFormat.MP4:
                    return "mp4";
                case ContainerFormat.MKV:
                    return "mkv";
                default:
                    throw new ArgumentOutOfRangeException(nameof(containerFormat), containerFormat, null);
            }
        }
    }
}