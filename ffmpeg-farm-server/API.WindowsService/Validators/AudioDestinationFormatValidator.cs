using Contract;
using FluentValidation;

namespace API.WindowsService.Validators
{
    public  class AudioDestinationFormatValidator : AbstractValidator<AudioDestinationFormat>
    {
        public AudioDestinationFormatValidator()
        {
            RuleFor(x => x.Format).NotEqual(ContainerFormat.Unknown);
            RuleFor(x => x.AudioCodec).NotEqual(Codec.Unknown);
            RuleFor(x => x.Bitrate).GreaterThan(0);
            RuleFor(x => x.Channels).NotEqual(Channels.Unknown);
        }
    }
}
