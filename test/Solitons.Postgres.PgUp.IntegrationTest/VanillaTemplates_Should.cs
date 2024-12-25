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
            .From(new Program());
        var templates = PgUpTemplateManager
            .GetTemplateDirectories()
                .ToList();
        Assert.True(templates.Count > 0, "The should be at least one template registered.");

        foreach (var template in templates)
        {
            var workingDir = Path
                .GetTempPath()
                .Convert(root => Path.Combine(root, Guid.NewGuid().ToString()))
                .Convert(Directory.CreateDirectory);

            var exitCode = processor.Process($@"pgup init ""{workingDir.FullName}""  --template {template.Name}");
            Assert.True(exitCode == 0, $"Template initialization failed with exit code {exitCode}");

            var pgUpProjectPath = Path.Combine(workingDir.FullName, "pgup.json");
            exitCode = processor.Process($@"pgup deploy ""{pgUpProjectPath}"" --overwrite --force --connection ""%{ConnectionStringKey}%""");
            Assert.True(exitCode == 0, $"Database deployment failed with exit code {exitCode}");
        }
    }
}