using System;
using API.WindowsService.Controllers;
using API.WindowsService.Test.Helpers;
using AutoFixture;
using AutoFixture.AutoMoq;
using Contract;
using Contract.Dto;
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
            var repository = fixture.Freeze<Mock<IJobRepository>>();
            var sut = fixture.Create<StatusController>();

            repository.Setup(m => m.Get(It.IsAny<Guid>()))
                .Returns((FFmpegJobDto) null);

            // Act
            var exception = Assert.Throws<ArgumentException>(() => sut.Get(Guid.NewGuid()));

            // Assert
            Assert.That(exception.ParamName, Is.EqualTo("id"));
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