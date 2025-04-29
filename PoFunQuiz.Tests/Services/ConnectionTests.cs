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
        public async Task OpenAI_Service_Should_Return_Questions()
        {
            // Arrange
            var openAISettings = new OpenAISettings
            {
                Endpoint = _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1",
                ApiKey = _configuration["OpenAI:ApiKey"] ?? "test-key",
                ModelName = _configuration["OpenAI:ModelName"] ?? "gpt-4",
                Temperature = 0.7f,
                MaxTokens = 2000
            };

            var options = Options.Create(openAISettings);
            var questionGenerator = new OpenAIQuestionGeneratorService(_logger, options);

            // Act
            var questions = await questionGenerator.GenerateQuestionsAsync(1);

            // Assert
            Assert.NotNull(questions);
            Assert.NotEmpty(questions);
            _output.WriteLine($"Successfully generated {questions.Count} question(s)");
            
            // Verify question structure
            var question = questions[0];
            Assert.NotNull(question.Question);
            Assert.NotEmpty(question.Question);
            Assert.NotNull(question.Options);
            Assert.Equal(4, question.Options.Count);
            Assert.InRange(question.CorrectOptionIndex, 0, 3);
            Assert.NotNull(question.Category);
            Assert.NotEmpty(question.Category);
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