
using Solitons.CommandLine.Reflection;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Solitons.CommandLine.Common;

public abstract class CliContractValidator<T> where T : class
{
    protected void Validate(
        Action<CliCommandExampleAttribute> onNotInvoked,
        Action<CliCommandExampleAttribute>? onInvoked = default)
    {
        var type = typeof(T);
        if (false == type.IsInterface)
        {
            throw new InvalidOperationException("Oops");
        }

        onInvoked ??= (tc) => Debug.WriteLine($"Passed: {tc.Example}");

        var testCases = typeof(T)
            .GetInterfaces()
            .Union([typeof(T)])
            .SelectMany(t => t.GetMethods())
            .Distinct()
            .SelectMany(mi => mi.GetCustomAttributes(true)
                .OfType<CliCommandExampleAttribute>())
            .ToArray();

        if (testCases.Length == 0)
        {
            throw new InvalidOperationException("Oops...");
        }

        T proxy = DispatchProxy.Create<T, CliRouteProxy>();

        var processor = CliProcessor
            .Create(config => config.AddService(proxy));

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

            if (targetMethod.ReturnType == typeof(void))
            {
                return 0;
            }

            // Return the default value for the method's return type
            if (targetMethod.ReturnType.IsValueType)
            {
                return Activator.CreateInstance(targetMethod.ReturnType);
            }

            if (typeof(Task).IsAssignableFrom(targetMethod.ReturnType))
            {
                if (targetMethod.ReturnType == typeof(Task))
                {
                    return Task.CompletedTask;
                }
                else if (targetMethod.ReturnType.IsGenericType &&
                         targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                    var defaultResult = Activator.CreateInstance(resultType);
                    return typeof(Task).GetMethod(nameof(Task.FromResult))?
                        .MakeGenericMethod(resultType)
                        .Invoke(null, new[] { defaultResult });
                }
            }
            return 0;
        }

        public override string ToString() => typeof(CliContractValidator<T>).ToString();
    }
}