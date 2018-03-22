using System.IO;
using API.WindowsService.Models;
using FluentValidation;

namespace API.WindowsService.Validators
{
    public class SreenshotRequestValidator : AbstractValidator<MuxJobRequestModel>
    {
        public SreenshotRequestValidator()
        {
            RuleFor(x => x.VideoSourceFilename).Must(File.Exists).WithMessage("File does not exist.");
        }
    }
}