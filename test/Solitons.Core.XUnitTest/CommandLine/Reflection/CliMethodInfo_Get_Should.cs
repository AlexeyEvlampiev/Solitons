using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Solitons.CommandLine.Reflection;

public sealed class CliMethodInfo_Get_Should
{
    [Fact]
    public void HandleTestCase0()
    {
        var methods = CliMethodInfo.Get(typeof(TestCase0));
        var loadReportMethod = methods.Single(m => m.Name.Equals(nameof(TestCase0.LoadReport)));
    }


    public sealed class TestCase0
    {
        [CliRoute("")]
        [CliArgument(nameof(filePath), "Demo file path argument")]
        [CliCommandExample("FY25-summary.csv")]
        public static void LoadReport(
            string filePath,
            CliFlag? verbose)
        {
            Debug.WriteLine($"Loading {filePath} file.");
        }

    }
}