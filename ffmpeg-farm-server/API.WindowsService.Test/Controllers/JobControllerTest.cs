using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using API.Database;
using API.WindowsService.Controllers;
using API.WindowsService.Models;
using API.WindowsService.Test.Helpers;
using AutoFixture;
using AutoFixture.AutoMoq;
using Contract;
using Moq;
using NUnit.Framework;

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
        public void DeleteShouldThrowForInvalidJobIdParameter()
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
        public void DeleteShouldReturnFalseWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var unitofwork = fixture.Freeze<Mock<IUnitOfWork>>();
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<JobController>();

            unitofwork.Setup(m => m.Jobs)
                .Returns(repository.Object);
            repository.Setup(m => m.Remove(It.IsAny<FfmpegJobs>()))
                .Returns(false);

            // Act
            var result = sut.DeleteJob(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void DeleteShouldReturnTrueWhenJobSuccessfullyDeleted()
        {
            // Arrange
            var jobCorrelationId = Guid.NewGuid();
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var unitofwork = fixture.Freeze<Mock<IUnitOfWork>>();
            var repository = new Mock<IJobRepository>();
            var sut = fixture.Create<JobController>();

            unitofwork.Setup(m => m.Jobs)
                .Returns(repository.Object);
            repository.Setup(m => m.Find(It.IsAny<Expression<Func<FfmpegJobs, bool>>>()))
                .Returns(new List<FfmpegJobs>
                {
                    new FfmpegJobs
                    {
                        JobCorrelationId = jobCorrelationId
                    }
                });
            repository.Setup(m => m.Remove(It.IsAny<FfmpegJobs>()))
                .Returns(true);

            // Act
            bool result = sut.DeleteJob(jobCorrelationId);

            // Assert
            Assert.That(result, Is.True);
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
            var unitofwork = fixture.Freeze<Mock<IUnitOfWork>>();
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<JobController>();

            unitofwork.Setup(m => m.Jobs)
                .Returns(repository.Object);
            repository.Setup(m => m.ResumeJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            var result = sut.PatchJob(Guid.NewGuid(), Command.Resume);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ResumeShouldReturnTrueWhenJobSuccessfullyResumed()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var unitofwork = fixture.Freeze<Mock<IUnitOfWork>>();
            var repository = new Mock<IJobRepository>();
            var sut = fixture.Create<JobController>();

            unitofwork.Setup(m => m.Jobs)
                .Returns(repository.Object);
            repository.Setup(m => m.ResumeJob(It.IsAny<Guid>()))
                .Returns(true);

            // Act
            bool result = sut.PatchJob(Guid.NewGuid(), Command.Resume);

            // Assert
            Assert.That(result, Is.True);
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
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            Mock<IJobRepository> repository = fixture.Freeze<Mock<IJobRepository>>();
            JobController sut = fixture.Create<JobController>();

            repository.Setup(m => m.PauseJob(It.IsAny<Guid>()))
                .Returns(false);

            // Act
            bool result = sut.PatchJob(Guid.NewGuid(), Command.Pause);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void PauseShouldReturnTrueWhenJobSuccessfullyPaused()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var unitofwork = fixture.Freeze<Mock<IUnitOfWork>>();
            var repository = new Mock<IJobRepository>();
            var sut = fixture.Create<JobController>();

            unitofwork.Setup(m => m.Jobs)
                .Returns(repository.Object);
            repository.Setup(m => m.PauseJob(It.IsAny<Guid>()))
                .Returns(true);

            // Act
            bool result = sut.PatchJob(Guid.NewGuid(), Command.Pause);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}