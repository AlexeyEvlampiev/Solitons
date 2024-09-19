using System.Diagnostics;
using System.Linq;

namespace Solitons.CommandLine.Common;

public abstract class CliCommandExamplesValidationTest
{
    protected void Validate()
    {
        var examples = Program
            .GetType()
            .GetMethods()
            .SelectMany(mi => mi.GetCustomAttributes(true)
                .OfType<CliCommandExampleAttribute>())
            .ToArray();


        var processor = ICliProcessor
            .Setup(config => config
                .UseCommandsFrom(Program));

        int expectedInvocationsCount = 0;
        foreach (var attribute in examples)
        {
            Debug.WriteLine(attribute.Example);
            Debug.WriteLine(attribute.Description);

            var commandLine = $"program {attribute.Example}";
            processor.Process($"program {attribute.Example}");
            expectedInvocationsCount++;
            OnResult(commandLine, expectedInvocationsCount);
        }
    }

    protected abstract void OnResult(string commandLine, int expectedInvocationsCount);

    protected abstract object Program { get; }


}