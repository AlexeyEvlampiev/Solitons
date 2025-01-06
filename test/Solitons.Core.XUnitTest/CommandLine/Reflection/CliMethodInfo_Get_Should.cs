﻿using System.ComponentModel;
using System.Linq;
using Xunit;

namespace Solitons.CommandLine.Reflection;

// ReSharper disable once InconsistentNaming
public sealed class CliMethodInfo_Get_Should
{
    [Fact]
    public void HandlePgUpCase()
    {
        IPgUpTestCase.Test();
    }

    public interface IPgUpTestCase
    {
        [CliRoute("init")]
        [CliArgument(nameof(projectDir), "Project directory path", Name = "ProjectDir")]
        [CliCommandExample("init .")]
        [CliCommandExample("init /src/database --template basic")]
        public void Initialize(
            string projectDir,
            [CliOption("--verbose")] CliFlag? verbose);

        internal static void Test()
        {
            var methods = CliMethodInfo.Get(typeof(IPgUpTestCase), CliContext.Empty);
            Assert.Equal(1, methods.Length);
            TestInitializeMethod(methods.Single(m => m.Name.Equals(nameof(Initialize))));
        }

        private static void TestInitializeMethod(CliMethodInfo method)
        {
            Assert.False(method.IsStatic);
            Assert.Equal(2, method.Examples.Length);

            var arguments = method.GetParameters().OfType<CliArgumentParameterInfo>().ToList();
            var options = method.GetParameters().OfType<CliOptionParameterInfo>().ToList();
            var optionBundles = method.GetParameters().OfType<CliOptionBundleParameterInfo>().ToList();

            Assert.Equal(1, arguments.Count);
            var filePathArgument = arguments.Single();
            Assert.Equal("projectDir", filePathArgument.Name);
            Assert.Equal("ProjectDir", filePathArgument.CliArgumentName);
            Assert.Equal("Project directory path", filePathArgument.Description);
            Assert.Equal(1, filePathArgument.CliRoutePosition);
            Assert.True(filePathArgument.TypeConverter is StringConverter);
        }
    }
}