using System.Net;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Integration tests verifying Content-Security-Policy and other security headers
/// are present on all key routes, preventing accidental removal.
/// </summary>
public class ContentSecurityPolicyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContentSecurityPolicyTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    [InlineData("/api/diag")]
    public async Task Response_HasContentSecurityPolicyHeader(string path)
    {
        // Act
        var response = await _client.GetAsync(path);

        // Assert — CSP header must be present regardless of status code
        Assert.True(
            response.Headers.Contains("Content-Security-Policy"),
            $"Expected Content-Security-Policy header on {path} (status {response.StatusCode})");
    }

    [Fact]
    public async Task RootPage_CspHeader_IsNotEmpty()
    {
        // Act
        var response = await _client.GetAsync("/");
        var csp = response.Headers.TryGetValues("Content-Security-Policy", out var values)
            ? string.Join("; ", values)
            : string.Empty;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(csp),
            "Content-Security-Policy header must not be empty");
    }

    [Fact]
    public async Task RootPage_CspHeader_ContainsDefaultSrc()
    {
        // Act
        var response = await _client.GetAsync("/");
        var csp = response.Headers.TryGetValues("Content-Security-Policy", out var values)
            ? string.Join("; ", values)
            : string.Empty;

        // Assert — 'default-src' is the baseline CSP directive
        Assert.Contains("default-src", csp,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/diag")]
    public async Task Response_HasXContentTypeOptionsHeader(string path)
    {
        // Act
        var response = await _client.GetAsync(path);

        // Assert — prevents MIME sniffing attacks
        var hasHeader = response.Headers.Contains("X-Content-Type-Options")
            || (response.Content.Headers.Contains("X-Content-Type-Options"));
        Assert.True(hasHeader,
            $"Expected X-Content-Type-Options header on {path}");
    }
}
