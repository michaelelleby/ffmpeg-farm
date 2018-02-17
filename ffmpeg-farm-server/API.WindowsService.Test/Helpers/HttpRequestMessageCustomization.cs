using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using AutoFixture;

namespace API.WindowsService.Test.Helpers
{
    internal class HttpRequestMessageCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<HttpRequestMessage>(c => c
                .Without(x => x.Content)
                .Do(x => x.Properties[HttpPropertyKeys.HttpConfigurationKey] =
                    new HttpConfiguration()));
        }
    }
}