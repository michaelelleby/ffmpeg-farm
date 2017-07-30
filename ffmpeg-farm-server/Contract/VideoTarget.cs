using System;

namespace Contract
{
    public class VideoTarget : IEquatable<VideoTarget>
    {
        public bool Equals(VideoTarget other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Bitrate == other.Bitrate && Equals(Size, other.Size) &&
                   string.Equals(Preset, other.Preset, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VideoTarget) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Bitrate;
                hashCode = (hashCode * 397) ^ (Size != null ? Size.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^
                           (Preset != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Preset) : 0);
                return hashCode;
            }
        }

        public int Bitrate { get; set; }
        public VideoSize Size { get; set; }
        public string Preset { get; set; }
    }
}