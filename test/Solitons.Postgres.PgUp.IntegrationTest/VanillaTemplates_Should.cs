using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

// ReSharper disable once InconsistentNaming
public class VanillaTemplates_Should
{
    [Fact]
    public async Task InitializeAndDeploy()
    {
        await EnvironmentInfo.TestConnectionAsync();

        var processor = CliProcessor
            .From(new Program());
        var templates = PgUpTemplateManager
            .GetTemplateDirectories()
                .ToList();
        Assert.True(templates.Count > 0);

        foreach (var template in templates)
        {
            var workingDir = Path
                .GetTempPath()
                .Convert(root => Path.Combine(root, Guid.NewGuid().ToString()))
                .Convert(Directory.CreateDirectory);

            var exitCode = processor.Process($@"pgup init ""{workingDir.FullName}""  --template {template.Name}");
            Assert.Equal(0, exitCode);
            var pgUpProjectPath = Path.Combine(workingDir.FullName, "pgup.json");
            exitCode = processor.Process($@"pgup deploy ""{pgUpProjectPath}"" --connection ""%DEV_POSTGRES_CONNECTION_STRING%"" --overwrite --force");
            Assert.Equal(0, exitCode);
        }
    }
}