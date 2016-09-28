using System;
using System.Net;
using System.Web.Http;
using API.WindowsService.Controllers;
using API.WindowsService.Test.Helpers;
using Contract;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace API.WindowsService.Test.Controllers
{
    [TestFixture]
    public class ResumeJobControllerTest
    {
        [Test]
        public void PauseShouldThrowForInvalidJobIdParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<ResumeController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Resume(Guid.Empty, JobType.Audio));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void PauseShouldThrowForInvalidTypeParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<ResumeController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Resume(Guid.NewGuid(), JobType.Unknown));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void ResumeShouldThrowWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<ResumeController>();

            repository.Setup(m => m.ResumeJob(It.IsAny<Guid>(), It.IsAny<JobType>()))
                .Returns(false);

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Resume(Guid.NewGuid(), JobType.Audio));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}