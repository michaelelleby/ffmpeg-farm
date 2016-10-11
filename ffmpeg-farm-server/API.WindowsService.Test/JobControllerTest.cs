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

namespace API.WindowsService.Test
{
    [TestFixture]
    public class JobControllerTest
    {
        [Test]
        public void GetInsertsClientHeartbeat()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            Mock<IHelper> mockHelper = fixture.Freeze<Mock<IHelper>>();
            var sut = fixture.Create<JobController>();

            // Act
            sut.GetNext("TESTMACHINE");

            // Assert
            mockHelper.Verify(m => m.InsertClientHeartbeat("TESTMACHINE"));
        }

        [Test]
        public void DeleteThrowsForInvalidJobId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<JobController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.DeleteJob(Guid.Empty));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void DeleteReturnsFalseIfJobWasNotFound()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<JobController>();

            repository.Setup(m => m.DeleteJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            var result = sut.DeleteJob(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ResumeShouldThrowForInvalidJobIdParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<JobController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.ResumeJob(Guid.Empty));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void ResumeShouldReturnFalseWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<JobController>();

            repository.Setup(m => m.ResumeJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            var result = sut.ResumeJob(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void PauseShouldThrowForInvalidJobIdParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<JobController>();

            // Act
            var exception = Assert.Throws<HttpResponseException>(() => sut.PauseJob(Guid.Empty));

            // Assert
            Assert.That(exception.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void PauseShouldReturnFalseWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<JobController>();

            repository.Setup(m => m.PauseJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            var result = sut.PauseJob(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }
    }
}