using Microsoft.Extensions.Options;

namespace PoFunQuiz.Core.Configuration;

public interface IConfigurationService
{
    AppSettings Settings { get; }
    T GetSection<T>() where T : class, new();
    OpenAISettings OpenAI { get; }
}

public class ConfigurationService : IConfigurationService
{
    private readonly IOptions<AppSettings> _options;
    private readonly IOptions<OpenAISettings> _openAIOptions;

    public ConfigurationService(
        IOptions<AppSettings> options,
        IOptions<OpenAISettings> openAIOptions)
    {
        _options = options;
        _openAIOptions = openAIOptions;
    }

    public AppSettings Settings => _options.Value;
    public OpenAISettings OpenAI => _openAIOptions.Value;

    public T GetSection<T>() where T : class, new()
    {
        var type = typeof(T);
        var property = typeof(AppSettings).GetProperty(type.Name.Replace("Settings", ""));
        return (T)(property?.GetValue(Settings) ?? new T());
    }
}