using System;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace Solitons.CommandLine.Reflection;

// ReSharper disable once InconsistentNaming
public sealed class CliMethodInfo_Get_Should
{
    [Fact]
    public void HandleTestCase0()
    {
        TestCase0.Test();
    }


    public sealed class TestCase0
    {
        [CliRoute("init")]
        [CliArgument(nameof(directoryPath), "Project directory path", Name = "ProjectDir")]
        [CliCommandExample("init .")]
        [CliCommandExample("init /src/database --template basic")]
        public static void LoadReport(
            string directoryPath,
            [CliOption("--verbose")] CliFlag? verbose) => throw new NotImplementedException();


        internal static void Test()
        {
            var methods = CliMethodInfo.Get(typeof(TestCase0));
            Assert.Equal(1, methods.Length);
            TestLoadReport(methods.Single(m => m.Name.Equals(nameof(LoadReport))));
        }

        private static void TestLoadReport(CliMethodInfo method)
        {
            Assert.True(method.IsStatic);
            Assert.Equal(2, method.Examples.Length);

            var arguments = method.GetParameters().OfType<CliArgumentParameterInfo>().ToList();
            var options = method.GetParameters().OfType<CliOptionParameterInfo>().ToList();
            var optionBundles = method.GetParameters().OfType<CliOptionBundleParameterInfo>().ToList();

            Assert.Equal(1, arguments.Count);
            var filePathArgument = arguments.Single();
            Assert.Equal("filePath", filePathArgument.Name);
            Assert.Equal("Report File Name", filePathArgument.CliArgumentName);
            Assert.Equal("Demo file path argument", filePathArgument.Description);
            Assert.Equal(0, filePathArgument.CliRoutePosition);
            Assert.True(filePathArgument.TypeConverter is StringConverter);

        }


    }
}