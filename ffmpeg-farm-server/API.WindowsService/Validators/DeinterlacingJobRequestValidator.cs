using System;
using System.IO;
using API.WindowsService.Models;
using FluentValidation;
using FluentValidation.Validators;

namespace API.WindowsService.Validators
{
    public class DeinterlacingJobRequestValidator : AbstractValidator<DeinterlacingJobRequestModel>
    {
        public DeinterlacingJobRequestValidator()
        {
            RuleFor(x => x.VideoSourceFilename).Must(File.Exists).WithMessage("File does not exist.");
        }

        private class OutputPathValidator : PropertyValidator
        {
            public OutputPathValidator(string errorMessage) : base(errorMessage)
            {
            }

            protected override bool IsValid(PropertyValidatorContext context)
            {
                var model = context.Instance as DeinterlacingJobRequestModel;
                if (model == null)
                    throw new ArgumentException(nameof(context));

                if (model.OutputFolder.Equals(Path.GetDirectoryName(model.VideoSourceFilename), StringComparison.InvariantCultureIgnoreCase)
                    && model.DestinationFilename.Equals(Path.GetFileName(model.VideoSourceFilename), StringComparison.InvariantCultureIgnoreCase))
                    return false;

                return true;
            }
        }
    }
}