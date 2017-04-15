using API.WindowsService.Models;
using Contract;
using FluentValidation;
using FluentValidation.Results;
using System.IO;

namespace API.WindowsService.Validators
{
    public class VideoRequestValidator : AbstractValidator<VideoJobRequestModel>
    {
        public VideoRequestValidator()
        {
            // Check that container format is set to a known format
            RuleFor(x => x.ContainerFormat).NotEqual(ContainerFormat.Unknown).WithMessage("Container format must be specified.");

            // Check if output folder exists
            Custom(x => Directory.Exists(x.OutputFolder) == false
                ? new ValidationFailure("OutputFolder", "Folder must be an existing folder")
                : null);

            // Check if video source file exists
            Custom(x => string.IsNullOrWhiteSpace(x.SourceFilename) || File.Exists(x.SourceFilename) == false
                ? new ValidationFailure("SourceFilename", "File is missing")
                : null);

            // If an audio source is specified, check if the file exists
            // Audio source is optional so only fail if one is specified
            // This will allow not specififing an external audio source file
            Custom(x => string.IsNullOrWhiteSpace(x.AudioSourceFilename) == false && File.Exists(x.AudioSourceFilename) == false
                ? new ValidationFailure("AudioSourceFilename", "File is missing")
                : null);

            Custom(x => x.Targets.Length > 0 == false
                ? new ValidationFailure("Targets", "At least one target must be specified")
                : null);

            RuleFor(x => x.Targets).SetCollectionValidator(new VideoDestinationFormatValidator());
        }
    }
}
