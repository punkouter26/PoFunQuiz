namespace PoFunQuiz.Web.Configuration;

public class AppSettings
{
    public StorageSettings Storage { get; set; } = new();
}

public class StorageSettings
{
    public string TableStorageConnectionString { get; set; } = string.Empty;
}
