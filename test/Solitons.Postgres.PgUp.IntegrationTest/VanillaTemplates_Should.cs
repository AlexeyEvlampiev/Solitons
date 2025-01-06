using Npgsql;
using Solitons.CommandLine;
using Solitons.Postgres.PgUp.Core;

namespace Solitons.Postgres.PgUp;
using static EnvironmentInfo;

// ReSharper disable once InconsistentNaming
public class VanillaTemplates_Should
{
    [Fact]
    public async Task InitializeAndDeploy()
    {
        await TestConnectionAsync();

        var processor = CliProcessor
            .Create(config => config
                .ConfigGlobalOptions(options => options
                    .Clear()
                    .Add(new CliTracingGlobalOptionBundle()))
                .AddService(new Program()));
        var templates = PgUpTemplateManager
            .GetTemplateDirectories()
                .ToList();
        Assert.True(templates.Count > 0, "The should be at least one template registered.");

        var csb = new NpgsqlConnectionStringBuilder(EnvironmentInfo.ConnectionString);
        foreach (var template in templates)
        {
            var workingDir = Path
                .GetTempPath()
                .Convert(root => Path.Combine(root, Guid.NewGuid().ToString()))
                .Convert(Directory.CreateDirectory);

            var exitCode = processor.Process($@"pgup init ""{workingDir.FullName}""  --template {template.Name} --trace Verbose");
            Assert.True(exitCode == 0, $"Template initialization failed with exit code {exitCode}");

            var pgUpProjectPath = Path.Combine(workingDir.FullName, "pgup.json");
            exitCode = processor.Process($@"pgup deploy ""{pgUpProjectPath}"" --overwrite --force --host {csb.Host} --port {csb.Port} --username ""{csb.Username}"" --password ""{csb.Password}""  --trace Verbose --timeout 00:03:00");
            Assert.True(exitCode == 0, $"Database deployment failed with exit code {exitCode}");
        }
    }
}