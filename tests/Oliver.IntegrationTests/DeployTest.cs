using FluentAssertions;
using Oliver.Common.Models;
using Oliver.IntegrationTests.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Oliver.IntegrationTests
{
    [Collection("Integration")]
    public class DeployTest : ClientServerTestBase
    {
        [Fact]
        public async Task Deploy_Should_Execute_Properly()
        {
            // Arrange
            var packageId = await CreatePackage();
            var templateId = await CreateTemplate();

            // Action

            // Assert
        }

        private async Task<long> CreatePackage()
        {
            var fileName = Path.Combine(solutionFolder, @"tools\samples\data", scriptArchiveFileName);
            if (!System.IO.File.Exists(fileName))
                throw AssertionException("File does not exists.", fileName);

            var id = await this.api.CreatePackageAsync(fileName, "1.0.1");
            this.errors.Should().BeEmpty();
            return id;
        }

        private async Task<long> CreateTemplate()
        {
            var template = new Template
            {
                Steps = new List<Template.Step>
                {
                    new Template.Step
                    {
                        Order = 1,
                        Name = "Extrack archive",
                        Type = Template.StepType.Archive,
                        FileName = scriptArchiveFileName
                    },
                    new Template.Step
                    {
                        Order = 2,
                        Name = "Run script from archive",
                        Type = Template.StepType.CMD,
                        Command = $@".\{scriptFileName}"
                    }
                }
            };

            var id = await this.api.CreateTemplateAsync(template);
            this.errors.Should().BeEmpty();
            return id;
        }
    }
}
