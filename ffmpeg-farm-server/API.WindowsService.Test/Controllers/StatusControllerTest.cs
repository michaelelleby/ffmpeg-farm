using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using API.Database;
using API.WindowsService.Controllers;
using API.WindowsService.Test.Helpers;
using AutoFixture;
using AutoFixture.AutoMoq;
using Contract;
using Moq;
using NUnit.Framework;

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
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => sut.Get(Guid.Empty));

            // Assert
            Assert.That(exception.ParamName, Is.EqualTo("id"));
        }

        [Test]
        public void GetShouldThrowWhenNoJobsFoundWithSpecifiedId()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var unitofwork = fixture.Freeze<Mock<IUnitOfWork>>();
            var jobsrepository = new Mock<IJobRepository>();
            var sut = fixture.Create<StatusController>();

            unitofwork.Setup(m => m.Jobs)
                .Returns(jobsrepository.Object);
            jobsrepository.Setup(m => m.Find(It.IsAny<Expression<Func<FfmpegJobs, bool>>>()))
                .Returns(new List<FfmpegJobs>());

            // Act
            var result = sut.Get(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void UpdateProgressShouldThrowForNullJobParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var sut = fixture.Create<StatusController>();

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => sut.UpdateProgress(null));

            // Assert
            Assert.That(exception.ParamName, Is.EqualTo("model"));
        }
    }
}