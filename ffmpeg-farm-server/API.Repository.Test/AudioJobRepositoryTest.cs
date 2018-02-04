using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Contract;
using NUnit.Framework;

namespace API.Repository.Test
{
    public class AudioJobRepositoryTest
    {
        [Test]
        public void AddThrowsForNullRequestParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            IAudioJobRepository sut = fixture.Create<AudioJobRepository>();

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => sut.Add(null, new List<AudioTranscodingJob>()));

            // Assert
        }

        [Test]
        public void AddThrowsForEmptyJobsParameter()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            IAudioJobRepository sut = fixture.Create<AudioJobRepository>();

            // Act
            var exception = Assert.Throws<ArgumentException>(() => sut.Add(new AudioJobRequest(), new List<AudioTranscodingJob>()));

            // Assert

        }

    }
}
