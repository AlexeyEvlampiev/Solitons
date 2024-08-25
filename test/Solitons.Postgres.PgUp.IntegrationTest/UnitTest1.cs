using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var processor = CliProcessor
            .Setup(options => options
                .UseCommandsFrom<Program>());
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