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
    public class AudioJobControllerTest
    {
        [Test]
        public void GetThrowsHttpResponseExceptionForEmptyMachineName()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var controller = fixture.Create<AudioJobController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => controller.Get(string.Empty));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void GetReturnsNextAvailableJob()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var controller = fixture.Create<AudioJobController>();

            // Act
            var result = controller.Get("TESTMACHINE");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetInsertsClientHeartbeat()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            Mock<IHelper> mockHelper = fixture.Freeze<Mock<IHelper>>();
            var sut = fixture.Create<AudioJobController>();

            // Act
            sut.Get("TESTMACHINE");

            // Assert
            mockHelper.Verify(m => m.InsertClientHeartbeat("TESTMACHINE"));
        }

        [Test]
        public void DeleteThrowsForInvalidJobId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<AudioJobController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Delete(Guid.Empty));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void DeleteThrowsIfJobWasNotFound()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<AudioJobController>();

            repository.Setup(m => m.DeleteJob(It.IsAny<Guid>(), JobType.Audio))
                .Returns(false);

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.Delete(Guid.NewGuid()));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}