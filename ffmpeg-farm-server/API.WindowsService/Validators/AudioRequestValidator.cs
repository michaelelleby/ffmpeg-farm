using System.IO;
using API.WindowsService.Models;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;

namespace API.WindowsService.Validators
{
    public class AudioRequestValidator : AbstractValidator<AudioJobRequestModel>
    {
        public AudioRequestValidator()
        {
            RuleFor(x => x.Targets).SetCollectionValidator(new AudioDestinationFormatValidator());
            Custom(x => Directory.Exists(x.OutputFolder) == false
                ? new ValidationFailure("OutputFolder", "Folder must be an existing folder")
                : null);
            Custom(x => File.Exists(x.SourceFilename) == false
                ? new ValidationFailure("SourceFilename", "File does not exist")
                : null);
        }
    }
}