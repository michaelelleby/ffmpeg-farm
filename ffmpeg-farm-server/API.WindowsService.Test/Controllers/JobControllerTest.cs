using System;
using API.WindowsService.Controllers;
using API.WindowsService.Models;
using API.WindowsService.Test.Helpers;
using Contract;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace API.WindowsService.Test.Controllers
{
    [TestFixture]
    public class JobControllerTest
    {

        [Test]
        public void DeleteThrowsForInvalidJobId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<JobController>();

            // Act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => sut.DeleteJob(Guid.Empty));

            // Assert
            Assert.That(exception.ParamName, Is.EqualTo("jobCorrelationId"));
        }

        [Test]
        public void DeleteReturnsFalseIfJobWasNotFound()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IOldJobRepository>>();
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
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => sut.PatchJob(Guid.Empty, Command.Resume));

            // Assert
            Assert.That(exception.ParamName, Is.EqualTo("jobCorrelationId"));
        }

        [Test]
        public void ResumeShouldReturnFalseWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IOldJobRepository>>();
            var sut = fixture.Create<JobController>();

            repository.Setup(m => m.ResumeJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            var result = sut.PatchJob(Guid.NewGuid(), Command.Resume);

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
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => sut.PatchJob(Guid.Empty, Command.Pause));

            // Assert
            Assert.That(exception.ParamName, Is.EqualTo("jobCorrelationId"));
        }

        [Test]
        public void PauseShouldReturnFalseWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repository = fixture.Freeze<Mock<IOldJobRepository>>();
            var sut = fixture.Create<JobController>();

            repository.Setup(m => m.PauseJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            var result = sut.PatchJob(Guid.NewGuid(), Command.Pause);

            // Assert
            Assert.That(result, Is.False);
        }
    }
}