using System;
using System.IO;
using API.WindowsService.Models;
using FluentValidation;
using FluentValidation.Validators;

namespace API.WindowsService.Validators
{
    public class MuxJobRequestValidator : AbstractValidator<MuxJobRequestModel>
    {
        public MuxJobRequestValidator()
        {
            RuleFor(x => x.VideoSourceFilename).Must(File.Exists).WithMessage("File does not exist.");
            RuleFor(x => x.AudioSourceFilename).Must(File.Exists).WithMessage("File does not exist.");

            RuleFor(x => x.VideoSourceFilename).SetValidator(new OutputPathValidator(
                "Destination folder must not be the same as input folder, when DestinationFilename is the same as VideoSourceFilename, since this would overwrite source file."));
            RuleFor(x => x.AudioSourceFilename).SetValidator(new OutputPathValidator(
                "Destination folder must not be the same as input folder, when DestinationFilename is the same as AudioSourceFilename, since this would overwrite source file."));
        }

        private class OutputPathValidator : PropertyValidator
        {
            public OutputPathValidator(string errorMessage) : base(errorMessage)
            {
            }

            protected override bool IsValid(PropertyValidatorContext context)
            {
                if (!(context.Instance is MuxJobRequestModel model))
                    throw new ArgumentException(nameof(context));

                if (model.OutputFolder.Equals(Path.GetDirectoryName(model.VideoSourceFilename), StringComparison.InvariantCultureIgnoreCase)
                    && model.DestinationFilename.Equals(Path.GetFileName(model.VideoSourceFilename), StringComparison.InvariantCultureIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(model.AudioSourceFilename)
                    && model.OutputFolder.Equals(Path.GetDirectoryName(model.AudioSourceFilename), StringComparison.InvariantCultureIgnoreCase)
                    && model.DestinationFilename.Equals(Path.GetFileName(model.AudioSourceFilename), StringComparison.InvariantCultureIgnoreCase))
                    return false;

                return true;
            }
        }
    }
}