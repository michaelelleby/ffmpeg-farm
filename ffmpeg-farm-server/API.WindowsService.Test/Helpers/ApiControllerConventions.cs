using AutoFixture;
using AutoFixture.AutoMoq;

namespace API.WindowsService.Test.Helpers
{
    internal class ApiControllerConventions : CompositeCustomization
    {
        /// <inheritdoc />
        internal ApiControllerConventions()
            : base(new ApiControllerConventions(),
                new AutoMoqCustomization())
        {
        }
    }
}