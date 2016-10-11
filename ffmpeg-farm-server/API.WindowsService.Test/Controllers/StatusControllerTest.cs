using System;
using System.Net;
using System.Web.Http;
using API.WindowsService.Controllers;
using API.WindowsService.Test.Helpers;
using Contract;
using Contract.Dto;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace API.WindowsService.Test.Controllers
{
    [TestFixture]
    public class StatusControllerTest
    {
        [Test]
        public void GetShouldThrowForInvalidIdParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<StatusController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Get(Guid.Empty));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void GetShouldThrowWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<StatusController>();

            repository.Setup(m => m.Get(It.IsAny<Guid>()))
                .Returns((FFmpegJobDto) null);

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Get(Guid.NewGuid()));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void UpdateProgressShouldThrowForNullJobParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<StatusController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.UpdateProgress(null));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}