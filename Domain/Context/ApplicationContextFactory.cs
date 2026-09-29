using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Domain.Context;

public sealed class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Path.Combine(currentDirectory, "API");
        var configurationDirectory = File.Exists(Path.Combine(apiDirectory, "appsettings.Development.json"))
            ? apiDirectory
            : currentDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("VersionZero")
            ?? throw new InvalidOperationException("The VersionZero connection string was not found.");

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationContext).Assembly.FullName))
            .Options;

        return new ApplicationContext(options);
    }
}
