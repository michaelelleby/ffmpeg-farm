using System;
using System.IO;
using API.WindowsService.Models;
using FluentValidation;
using FluentValidation.Validators;

namespace API.WindowsService.Validators
{
    public class HardSubtitlesJobRequestValidator: AbstractValidator<HardSubtitlesJobRequestModel>
    {
        public HardSubtitlesJobRequestValidator()
        {

            RuleFor(x => x.VideoSourceFilename).Must(File.Exists).WithMessage("File does not exist.");
            RuleFor(x => x.SubtitlesFilename).Must(File.Exists).WithMessage("File does not exist.");

            RuleFor(x => x.VideoSourceFilename).SetValidator(new OutputPathValidator(
                "Destination folder must not be the same as input folder, when DestinationFilename is the same as VideoSourceFilename, since this would overwrite source file."));
            RuleFor(x => x.SubtitlesFilename).SetValidator(new OutputPathValidator(
                "Destination folder must not be the same as input folder, when DestinationFilename is the same as AudioSourceFilename, since this would overwrite source file."));
        }
        // copy-pasted from mux validator
        private class OutputPathValidator : PropertyValidator
        {
            public OutputPathValidator(string errorMessage) : base(errorMessage)
            {
            }

            protected override bool IsValid(PropertyValidatorContext context)
            {
                var model = context.Instance as HardSubtitlesJobRequestModel;
                if (model == null)
                    throw new ArgumentException(nameof(context));

                if (model.OutputFolder.Equals(Path.GetDirectoryName(model.VideoSourceFilename), StringComparison.InvariantCultureIgnoreCase)
                    && model.DestinationFilename.Equals(Path.GetFileName(model.VideoSourceFilename), StringComparison.InvariantCultureIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(model.SubtitlesFilename)
                    && model.OutputFolder.Equals(Path.GetDirectoryName(model.SubtitlesFilename), StringComparison.InvariantCultureIgnoreCase)
                    && model.DestinationFilename.Equals(Path.GetFileName(model.SubtitlesFilename), StringComparison.InvariantCultureIgnoreCase))
                    return false;

                return true;
            }
        }
    }
}