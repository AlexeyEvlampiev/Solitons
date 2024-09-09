using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    private readonly CliDeserializer[] _parameterDeserializers;
    private readonly CliMasterOptionBundle[] _masterOptionBundles;
    private readonly ActionHandler _handler;
    private readonly ICliActionSchema _schema;

    delegate Task<int> ActionHandler(object?[] args);

    private CliAction(
        ActionHandler handler,
        CliDeserializer[] parameterDeserializers,
        CliMasterOptionBundle[] masterOptionBundles,
        ICliActionSchema schema)
    {
        _parameterDeserializers = parameterDeserializers;
        _masterOptionBundles = masterOptionBundles;
        _handler = ThrowIf.ArgumentNull(handler);
        _schema = ThrowIf.ArgumentNull(schema);
    }



    internal static CliAction Create(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptionBundles,
        CliRouteAttribute[] baseRoutes)
    {
        ThrowIf.ArgumentNull(method);
        ThrowIf.ArgumentNull(masterOptionBundles);
        ThrowIf.ArgumentNull(baseRoutes);



        var parameters = method.GetParameters();
        var methodAttributes = method.GetCustomAttributes().ToList();
        var argumentInfos = new Dictionary<CliRouteArgumentAttribute, JazzArgumentInfo>();

        var arguments = methodAttributes
            .OfType<CliRouteArgumentAttribute>()
            .Select(arg => new
            {
                Argument = arg,
                Selection = parameters.Where(arg.References).ToList()
            })
            .Do(item =>
            {
                var (argument, selection) = (item.Argument, Parameters: item.Selection);
                if (selection.Count == 0)
                {
                    throw new CliConfigurationException(
                        $"The parameter '{argument.ParameterName}', referenced by the '{argument.GetType().Name}' attribute in the '{method.Name}' method, " +
                        $"could not be found in the method signature. " +
                        $"Verify that the parameter name is correct and matches the method's defined parameters.");
                }


                if (selection.Count > 1)
                {
                    throw new CliConfigurationException(
                        $"The parameter '{argument.ParameterName}' in the '{method.Name}' method is referenced by more than one '{argument.GetType().Name}' attribute. " +
                        $"Each parameter can only be referenced by one attribute. Ensure that only a single attribute is applied to each method parameter.");
                }

                var parameterType = selection.Select(p => p.ParameterType).Single();
                if (CliOptionBundle.IsAssignableFrom(parameterType))
                {
                    throw new CliConfigurationException(
                        $"The parameter '{argument.ParameterName}' in the '{method.Name}' method is an option bundle of type '{parameterType}'. " +
                        $"Option bundles cannot be marked as command arguments. Review the method signature and ensure that bundles are handled correctly.");
                }

            })
            .Select(item => new
            {
                Argument = item.Argument,
                Parameter = item.Selection.Single(),
                Info = new JazzArgumentInfo(item.Argument, item.Selection.Single())
            })
            .Do(item =>
            {
                argumentInfos.Add(item.Argument, item.Info);
            })
            .ToDictionary(item => item.Parameter, item => item.Info);


        var parameterDeserializers = new List<CliDeserializer>();
        var options = new List<JazzyOptionInfo>();
        for (int i = 0; i < parameters.Length; ++i)
        {
            var parameter = parameters[i];
            var parameterAttributes = parameter.GetCustomAttributes().ToList();
            var optionAttribute = parameterAttributes.OfType<CliOptionAttribute>().SingleOrDefault();
            var description = parameterAttributes
                .OfType<DescriptionAttribute>()
                .Select(attribute => attribute.Description)
                .Union(parameterAttributes.OfType<CliOptionAttribute>().Select(attribute => attribute.Description))
                .Union([$"'{method.Name}' method parameter."])
                .First();

            bool isBundle = CliOptionBundle.IsAssignableFrom(parameter.ParameterType);

            if (isBundle)
            {
                if (optionAttribute is not null)
                {
                    throw new CliConfigurationException(
                        $"The parameter '{parameter.Name}' in the '{method.Name}' method is an option bundle of type '{parameter.ParameterType}'. " +
                        $"Option bundles cannot be marked as individual options. Review the method signature and ensure that bundles are handled correctly.");
                }
                ThrowIf.True(arguments.ContainsKey(parameter));
                parameterDeserializers[i] = CliOptionBundle.GetDeserializerFor(parameter.ParameterType);
                options.AddRange(CliOptionBundle.GetOptions(parameter.ParameterType));
            }
            else
            {
                if (arguments.TryGetValue(parameter, out var argument))
                {
                    if (optionAttribute is not null)
                    {
                        throw new CliConfigurationException(
                            $"The parameter '{parameter.Name}' in the '{method.Name}' method is marked as both a command-line option and " +
                            $"a command-line argument. A parameter cannot be marked as both. Please review the attributes applied to this parameter."
                        );
                    }

                    parameterDeserializers[i] = argument.Deserialize;
                }
                else
                {
                    optionAttribute ??= new CliOptionAttribute(
                        ThrowIf.NullOrWhiteSpace(parameter.Name),
                        description);

                    var option = new JazzyOptionInfo(
                        ThrowIf.NullReference(optionAttribute),
                        parameter.DefaultValue,
                        description,
                        parameter.ParameterType)
                    {
                        IsRequired = (parameter.HasDefaultValue == false) ||
                                     parameterAttributes.OfType<RequiredAttribute>().Any()
                    };
                    options.Add(option);
                    parameterDeserializers[i] = option.Deserialize;
                }
            }

        }

        foreach (var bundle in masterOptionBundles)
        {
            options.AddRange(bundle.GetOptions());
        }

        var schemaMetadata = new List<object>()
        {
            baseRoutes.SelectMany(route => route)
        };
        foreach (var methodAttribute in methodAttributes)
        {
            if (methodAttribute is CliRouteAttribute route)
            {
                schemaMetadata.AddRange(route);
            }

            if (methodAttribute is CliRouteArgumentAttribute arg)
            {
                var info = argumentInfos[arg];
                schemaMetadata.Add(info);
            }
        }

        schemaMetadata.AddRange(options);
        schemaMetadata.AddRange(methodAttributes.OfType<CliCommandExampleAttribute>());
        

        ICliActionSchema schema = new CliActionSchema(schemaMetadata);


        ThrowIf.False(parameters.Length == parameterDeserializers.Count);
        return new CliAction(InvokeAsync, parameterDeserializers.ToArray(), masterOptionBundles, schema);

        [DebuggerStepThrough]
        async Task<int> InvokeAsync(object?[] args)
        {
            Debug.WriteLine($"Invoking '{method.Name}'");
            var result = method.Invoke(instance, args);
            Debug.WriteLine($"Invoking '{method.Name}' returned '{result}'");
            if (result is Task task)
            {
                Debug.WriteLine($"Awaiting '{method.Name}' returned task");
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task) ?? 0;
                    Debug.WriteLine($"'{method.Name}' returned task result is '{result}'");
                }
            }

            if (result is int exitCode)
            {
                return exitCode;
            }

            return 0;
        }
    }


    public int Execute(string commandLine, CliTokenDecoder decoder)
    {
        commandLine = ThrowIf.ArgumentNullOrWhiteSpace(commandLine);
        var match = _schema.Match(
            commandLine, 
            decoder, 
            unmatchedTokens =>
        {
            var csv = unmatchedTokens
                .Join(", ");
            CliExit.With(
                $"The following options are not recognized as valid for the command: {csv}. " +
                $"Please check the command syntax.");
        });

        if (match.Success == false)
        {
            throw new InvalidOperationException($"The command line did not match any known patterns.");
        }

        var args = new object?[_parameterDeserializers.Length];
        for (int i = 0; i < args.Length; ++i)
        {
            var deserializer = _parameterDeserializers[i];
            args[i] = deserializer.Invoke(match, decoder);
        }

        var masterBundles = _masterOptionBundles.Select(bundle => bundle.Clone()).ToList();
        foreach (var bundle in masterBundles)
        {
            bundle.PopulateOptions(match, decoder);
        }

        masterBundles.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var task = _handler.Invoke(args);
            task.GetAwaiter().GetResult();
            var resultProperty = task.GetType().GetProperty("Result");
            object result = 0;
            if (resultProperty != null)
            {
                result = resultProperty.GetValue(task) ?? 0;
            }

            masterBundles.ForEach(bundle => bundle.OnActionExecuted(commandLine));
            
            return result is int exitCode ? exitCode : 0;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.GetType().Name);
            masterBundles.ForEach(bundle => bundle.OnError(commandLine, e));
            throw;
        }


    }

    [DebuggerStepThrough]
    public int Rank(string commandLine) => _schema.Rank(commandLine);


    [DebuggerStepThrough]
    public bool IsMatch(string commandLine) => _schema.IsMatch(commandLine);

    public void ShowHelp()
    {
        Console.WriteLine(GetHelpText());
    }


    public int CompareTo(CliAction? other)
    {
        other = ThrowIf.ArgumentNull(other, "Cannot compare to a null object.");
        return String.Compare(_schema.CommandRouteExpression, other._schema.CommandRouteExpression, StringComparison.OrdinalIgnoreCase);
    }


    public override string ToString() => _schema.CommandRouteExpression;

    public string GetHelpText() => _schema.GetHelpText();

    public ICliActionSchema GetSchema() => _schema;
}