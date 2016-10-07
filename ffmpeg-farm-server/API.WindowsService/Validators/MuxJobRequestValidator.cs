using System.IO;
using API.WindowsService.Models;
using FluentValidation;

namespace API.WindowsService.Validators
{
    public class MuxJobRequestValidator : AbstractValidator<MuxJobRequestModel>
    {
        public MuxJobRequestValidator()
        {
            RuleFor(x => x.VideoSourceFilename).Must(File.Exists).WithMessage("File does not exist.");
            RuleFor(x => x.AudioSourceFilename).Must(File.Exists).WithMessage("File does not exist.");
        }
    }
}