
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine.Common;

public abstract class CliRouteTest<T> where T : class
{
    protected CliRouteTest()
    {
        var contract = typeof(T);
        if (false == contract.IsInterface)
        {
            throw new InvalidOperationException($"{typeof(T).Name} must be an interface.");
        }
    }

    protected void TestExamples(
        Action<CliCommandExampleAttribute> onNotInvoked,
        Action<CliCommandExampleAttribute>? onInvoked = default)
    {
        onInvoked ??= (tc) => Debug.WriteLine($"Passed: {tc.Example}");

        var testCases = typeof(T)
            .GetMethods()
            .SelectMany(mi => mi.GetCustomAttributes(true)
                .OfType<CliCommandExampleAttribute>())
            .ToArray();

        if (testCases.Length == 0)
        {
            throw new InvalidOperationException("Oops...");
        }

        T proxy = DispatchProxy.Create<T, CliRouteProxy>();

        var processor = ICliProcessor
            .Setup(config => config
                .UseDescription("Test")
                .UseCommandsFrom(proxy));

        foreach (var testCase in testCases)
        {
            Debug.WriteLine(testCase.Example);
            Debug.WriteLine(testCase.Description);

            var commandLine = $"program {testCase.Example}";
            int result = processor.Process($"program {testCase.Example}");
            if (result == 0)
            {
                onInvoked(testCase);
            }
            else
            {
                onNotInvoked(testCase);
            }
        }
    }




    private class CliRouteProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            // Check if the method has the required attribute
            var attributes = targetMethod.GetCustomAttributes(typeof(CliCommandExampleAttribute), true);
            if (attributes.Length == 0)
            {
                throw new InvalidOperationException($"Method {targetMethod.Name} is not adorned with CliCommandExampleAttribute.");
            }

            // Return the default value for the method's return type
            if (targetMethod.ReturnType.IsValueType)
            {
                return Activator.CreateInstance(targetMethod.ReturnType);
            }

            return 0;
        }
    }
}