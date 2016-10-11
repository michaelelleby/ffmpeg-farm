using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class TaskControllerTest
    {
        [Test]
        public void GetInsertsClientHeartbeat()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            Mock<IHelper> mockHelper = fixture.Freeze<Mock<IHelper>>();
            var sut = fixture.Create<TaskController>();

            // Act
            sut.GetNext("TESTMACHINE");

            // Assert
            mockHelper.Verify(m => m.InsertClientHeartbeat("TESTMACHINE"));
        }
    }
}
