using System.IO;
using System.Linq;
using API.WindowsService.Models;
using FluentValidation;
using FluentValidation.Results;

namespace API.WindowsService.Validators
{
    public class AudioRequestValidator : AbstractValidator<AudioJobRequestModel>
    {
        public AudioRequestValidator()
        {
            Custom(x => x.Targets.Length > 0 == false
                ? new ValidationFailure("Targets", "At least one target must be specified")
                : null);
            RuleFor(x => x.Targets).SetCollectionValidator(new AudioDestinationFormatValidator());
            Custom(x => Directory.Exists(x.OutputFolder) == false
                ? new ValidationFailure("OutputFolder", "Folder must be an existing folder")
                : null);
            Custom(x => File.Exists(x.SourceFilenames.First()) == false
                ? new ValidationFailure("SourceFilenames", "File does not exist")
                : null);
        }
    }
}