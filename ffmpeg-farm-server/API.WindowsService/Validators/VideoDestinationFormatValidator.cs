using Contract;
using FluentValidation;

namespace API.WindowsService.Validators
{
    public class VideoDestinationFormatValidator : AbstractValidator<VideoDestinationFormat>
    {
        public VideoDestinationFormatValidator()
        {
            RuleFor(x => x.VideoBitrate).GreaterThan(0);
            RuleFor(x => x.Profile).NotEqual(H264Profile.Unknown).WithMessage("H264 profile must be set");
            RuleFor(x => x.Width).GreaterThan(0);
            RuleFor(x => x.Height).GreaterThan(0);
            RuleFor(x => x.OutputExtension).NotEmpty();
            RuleFor(x => x.AudioBitrate).GreaterThanOrEqualTo(0);
        }
    }
}