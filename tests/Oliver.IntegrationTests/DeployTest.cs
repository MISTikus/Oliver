using Oliver.Common.Models;
using Oliver.IntegrationTests.Helpers;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
            await CreatePackage();
            var templateId = await CreateTemplate();

            // Action

            // Assert

        }

        private async Task CreatePackage()
        {
            var fileName = Path.Combine(solutionFolder, @"samples\data", scriptArchiveFileName);
            if (!System.IO.File.Exists(fileName))
                throw base.AssertionException("File does not exists.", fileName);
            /*
            var request = new RestRequest(templatesApi)
            {
                RequestFormat = DataFormat.Json,
                AlwaysMultipartFormData = true
            };

            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddParameter("Version", "1.0.1", ParameterType.RequestBody);
            request.AddFile(fileName, fileName, "application/zip");
            request.AddParameter("Body", fileName, "application/zip", ParameterType.RequestBody);

            var response = await base.restClient.ExecutePostAsync(request);
            if (!response.IsSuccessful)
                throw base.AssertionException("Failed to create package.", response.ErrorMessage);
            */

            var client = new HttpClient { BaseAddress = new Uri(apiHost) };
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(fileName));
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = scriptArchiveFileName,
                Name = "Body"
            };
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent);

            content.Add(new StringContent("1.0.1"), "Version");
            var response = await client.PostAsync(packagesApi, content);
            if (!response.IsSuccessStatusCode)
                throw base.AssertionException("Failed to create package.", await response?.Content?.ReadAsStringAsync());
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
            var request = new RestRequest(templatesApi);
            request.AddJsonBody(template);
            var idResponse = await base.restClient.ExecutePostAsync<long>(request);
            if (!idResponse.IsSuccessful)
                throw base.AssertionException($"Failed to create template.", idResponse.ErrorMessage);
            return idResponse.Data;
        }
    }
}
