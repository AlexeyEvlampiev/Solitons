using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var processor = CliProcessor
                .Setup(options => options
                    .UseCommandsFrom<Program>());
            var templates =
                Path
                    .Combine(".", "templates")
                    .Convert(path => new DirectoryInfo(path))
                    .Convert(dir =>
                    {
                        Assert.True(dir.Exists);
                        return dir
                            .GetDirectories("*", SearchOption.AllDirectories)
                            .Where(t => t
                                .EnumerateFiles("pgup.json")
                                .Any())
                            .Select(t => Path.GetRelativePath(dir.FullName, t.FullName));
                    })
                    .ToList();
            Assert.True(templates.Count > 0);
            
            foreach (var template in templates)
            {
                var workingDir = Path
                    .GetTempPath()
                    .Convert(root => Path.Combine(root, Guid.NewGuid().ToString()))
                    .Convert(Directory.CreateDirectory);

                var exitCode = processor.Process($@"pgup init ""{workingDir.FullName}""  --template {template}");
                Assert.Equal(0, exitCode);
                var pgUpProjectPath = Path.Combine(workingDir.FullName, "pgup.json");
                exitCode = processor.Process($@"pgup deploy ""{pgUpProjectPath}"" --connection ""%DEV_POSTGRES_CONNECTION_STRING%"" --overwrite --force");
                Assert.Equal(0, exitCode);
            }
        }
    }
}