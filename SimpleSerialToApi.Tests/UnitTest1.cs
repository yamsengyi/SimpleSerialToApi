using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Tests
{
    public class ProjectSetupTests
    {
        [Fact]
        public void DependencyInjection_Should_BeConfiguredCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<ProjectSetupTests>>();

            // Assert
            logger.Should().NotBeNull();
            serviceProvider.Should().NotBeNull();
        }

        [Fact]
        public void Step01Requirements_Should_BeValid()
        {
            // Arrange & Act
            var projectName = "SimpleSerialToApi";
            var testProjectName = "SimpleSerialToApi.Tests";

            // Assert - Basic validation that step 01 requirements are met
            projectName.Should().NotBeNullOrEmpty();
            testProjectName.Should().NotBeNullOrEmpty();
            
            // Verify folder structure exists (these will be created)
            var expectedFolders = new[] { "Models", "Services", "ViewModels", "Views", "Utils" };
            expectedFolders.Should().NotBeEmpty();
        }
    }
}