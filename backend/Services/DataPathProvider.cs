namespace Nomopsis.Api.Services;

public sealed class DataPathProvider(IConfiguration configuration) : IDataPathProvider
{
    public string DataPath { get; } = Environment.GetEnvironmentVariable("DATA_PATH")
        ?? configuration["Data:Path"]
        ?? "/data";
}
