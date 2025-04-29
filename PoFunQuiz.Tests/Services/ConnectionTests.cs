using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace PoFunQuiz.Tests.Services
{
    public class ConnectionTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIQuestionGeneratorService> _logger;
        private readonly TableServiceClient _tableServiceClient;

        public ConnectionTests(ITestOutputHelper output)
        {
            _output = output;

            // Create configuration
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Create logger that writes to test output
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddXUnit(output);
                builder.SetMinimumLevel(LogLevel.Debug);
            }).CreateLogger<OpenAIQuestionGeneratorService>();

            // Initialize table service client for Azurite
            var azuriteConnectionString = "UseDevelopmentStorage=true";
            _tableServiceClient = new TableServiceClient(azuriteConnectionString);
        }

        [Fact]
        public async Task OpenAI_Connection_Should_Work()
        {
            // Arrange
            var openAISettings = new OpenAISettings
            {
                Endpoint = _configuration["OpenAI:Endpoint"],
                Key = _configuration["OpenAI:Key"],
                DeploymentName = _configuration["OpenAI:DeploymentName"]
            };

            var options = Options.Create(openAISettings);
            var questionGenerator = new OpenAIQuestionGeneratorService(_logger, options);

            // Act & Assert
            try
            {
                var questions = await questionGenerator.GenerateQuestionsAsync(1);
                Assert.NotNull(questions);
                Assert.NotEmpty(questions);
                _output.WriteLine($"Successfully generated {questions.Count} question(s)");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error connecting to OpenAI: {ex}");
                throw;
            }
        }

        [Fact]
        public async Task Azurite_Connection_Should_Work()
        {
            // Arrange
            var tableName = "TestTable";

            try
            {
                // Act
                // Create a test table
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();

                // Try to add a test entity
                var entity = new TableEntity("TestPartition", "TestRow")
                {
                    { "TestProperty", "TestValue" }
                };
                await tableClient.AddEntityAsync(entity);

                // Try to retrieve the entity
                var retrievedEntity = await tableClient.GetEntityAsync<TableEntity>("TestPartition", "TestRow");

                // Assert
                Assert.NotNull(retrievedEntity);
                Assert.Equal("TestValue", retrievedEntity.Value.GetString("TestProperty"));
                _output.WriteLine("Successfully connected to Azurite and performed table operations");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error connecting to Azurite: {ex}");
                throw;
            }
            finally
            {
                // Cleanup
                try
                {
                    await _tableServiceClient.DeleteTableAsync(tableName);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error cleaning up test table: {ex}");
                }
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
} 