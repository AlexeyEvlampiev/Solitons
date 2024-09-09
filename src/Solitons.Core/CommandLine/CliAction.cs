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
    private readonly ParameterInfo[] _parameters;
    private readonly JazzyOptionInfo[] _options;
    private readonly CliDeserializer[] _parameterDeserializers;


    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Func<object?[], Task<int>> _actionHandler;
    private readonly CliMasterOptionBundle[] _masterOptions;
    private readonly ICliActionSchema _schema;
    private readonly ICliCommandMethodParametersFactory _parametersFactory;

    public CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptionBundles,
        IEnumerable<CliRouteAttribute> baseRoutes)
    {
        ThrowIf.ArgumentNull(method);
        ThrowIf.ArgumentNull(masterOptionBundles);
        ThrowIf.ArgumentNull(baseRoutes);



        _parameters = method.GetParameters();

        var methodAttributes = method.GetCustomAttributes().ToList();
        
        var arguments = methodAttributes
            .OfType<CliRouteArgumentAttribute>()
            .Select(arg => new
            {
                Argument = arg,
                Selection = _parameters.Where(arg.References).ToList()
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
                Parameter = item.Selection.Single()
            })
            .ToDictionary(item => item.Parameter, item => new JazzArgumentInfo(item.Argument, item.Parameter));


        var parameterDeserializers = new List<CliDeserializer>();
        var options = new List<JazzyOptionInfo>();
        for(int i = 0; i < _parameters.Length; ++i)
        {
            var parameter = _parameters[i];
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

        _options = options.ToArray();
        _parameterDeserializers = parameterDeserializers.ToArray();

        ThrowIf.False(_parameters.Length == _parameterDeserializers.Length);
    }

    [DebuggerNonUserCode]
    internal CliAction(
        Func<object?[], Task<int>> actionHandler,
        ICliActionSchema schema,
        ICliCommandMethodParametersFactory parametersFactory,
        CliMasterOptionBundle[] masterOptions)
    {
        _actionHandler = actionHandler;
        _schema = schema;
        _parametersFactory = parametersFactory;
        _masterOptions = masterOptions;
    }


    internal static CliAction Create(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptionBundles,
        IEnumerable<CliRouteAttribute> baseRouteMetadata)
    {
        ThrowIf.ArgumentNull(method);
        ThrowIf.ArgumentNull(masterOptionBundles);

        var schema = new CliActionSchema(method, masterOptionBundles, baseRouteMetadata);
        var parametersFactory = new CliActionHandlerParametersFactory(method);
        return new CliAction(InvokeAsync, schema, parametersFactory, masterOptionBundles);

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

        var args = _parametersFactory.BuildMethodArguments(match, decoder);

        foreach (var bundle in _masterOptions)
        {
            bundle.PopulateFrom(match, decoder);
        }

        _masterOptions.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var task = _actionHandler.Invoke(args);
            task.GetAwaiter().GetResult();
            var resultProperty = task.GetType().GetProperty("Result");
            object result = 0;
            if (resultProperty != null)
            {
                result = resultProperty.GetValue(task) ?? 0;
            }

            _masterOptions.ForEach(bundle => bundle.OnActionExecuted(commandLine));
            
            return result is int exitCode ? exitCode : 0;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.GetType().Name);
            _masterOptions.ForEach(bundle => bundle.OnError(commandLine, e));
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