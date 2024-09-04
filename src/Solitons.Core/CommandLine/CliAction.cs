using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    delegate object? ValueBinder(Match commandLineMatch);


    abstract class CliParameterInfo
    {
        protected CliParameterInfo(
            ParameterInfo parameterInfo,
            MethodInfo methodInfo,
            IReadOnlyList<Attribute> methodAttributes,
            CliTokenSubstitutionPreprocessor preprocessor)
        {
            ParameterInfo = parameterInfo;
            MethodInfo = methodInfo;
            MethodAttributes = methodAttributes;
            Preprocessor = preprocessor;
        }

        public static CliParameterInfo Create(
            ParameterInfo parameterInfo,
            MethodInfo methodInfo,
            IReadOnlyList<Attribute> methodAttributes,
            CliTokenSubstitutionPreprocessor preprocessor)
        {
            return CliOptionBundle.IsAssignableFrom(parameterInfo.ParameterType)
                ? new CliExplicitParameterInfo(parameterInfo, methodInfo, methodAttributes, preprocessor)
                : new CliOptionBundleParameterInfo(parameterInfo, methodInfo, methodAttributes, preprocessor);
        }

        public abstract object? Bind(Match commandLineMatch);

        public abstract IEnumerable<JazzyOption> AsBundle();

        public ParameterInfo ParameterInfo { get; }
        public MethodInfo MethodInfo { get; }
        public IReadOnlyList<Attribute> MethodAttributes { get; }
        public CliTokenSubstitutionPreprocessor Preprocessor { get; }

        protected CliOptionAttribute? FindOptionMetadata() => MethodAttributes.OfType<CliOptionAttribute>().SingleOrDefault();

        protected CliRouteArgumentAttribute? FindArgumentMetadata() => MethodAttributes
            .OfType<CliRouteArgumentAttribute>()
            .Where(argument => argument.References(ParameterInfo))
            .Do((args, count) =>
            {
                if (count > 0)
                {
                    throw new InvalidOperationException("Oops...");
                }
            })
            .SingleOrDefault();
    }


    sealed class CliOptionBundleParameterInfo : CliParameterInfo
    {
        private readonly List<JazzyOption> _options = new(10);
        public CliOptionBundleParameterInfo(
            ParameterInfo parameterInfo,
            MethodInfo methodInfo,
            IReadOnlyList<Attribute> methodInfoAttributes,
            CliTokenSubstitutionPreprocessor preprocessor) : base(parameterInfo, methodInfo, methodInfoAttributes, preprocessor)
        {
            Debug.Assert(CliOptionBundle.IsAssignableFrom(parameterInfo.ParameterType));
            var argument = FindArgumentMetadata();
            var option = FindOptionMetadata();
            if (argument is not null)
            {
                throw new InvalidOperationException("Option bundles cannot be declared as cli route arguments.");
            }

            if (option is not null)
            {
                throw new InvalidOperationException("Option bundles cannot be declared as cli option.");
            }

            foreach (var property in OptionBundleType.GetProperties())
            {
                if (CliOptionBundle.IsAssignableFrom(property.PropertyType))
                {
                    throw new InvalidOperationException("Nested option bundles are not supported.");
                }
                var attributes = property.GetCustomAttributes().ToList();
                option = attributes.OfType<CliOptionAttribute>().SingleOrDefault();
                if (option is null)
                {
                    continue;
                }

                var description = attributes.OfType<DescriptionAttribute>().Select(a => a.Description)
                    .Concat([option.Description, $"{OptionBundleType}.{property.Name} property"])
                    .First();

                var required = attributes.OfType<RequiredAttribute>().Any();
                var bundle = Activator.CreateInstance(OptionBundleType) ?? throw new InvalidOperationException();
                var defaultValue = property.GetValue(bundle, []);
                _options.Add(new JazzyOption(option, defaultValue, description, property.PropertyType)
                {
                    IsRequired = required,
                    Preprocessor = Preprocessor
                });
            }

        }

        public Type OptionBundleType => ParameterInfo.ParameterType;

        public override object? Bind(Match commandLineMatch)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<JazzyOption> AsBundle() => _options.AsEnumerable();
    }

    sealed class CliExplicitParameterInfo : CliParameterInfo
    {
        public CliExplicitParameterInfo(
            ParameterInfo parameterInfo,
            MethodInfo methodInfo,
            IReadOnlyList<Attribute> methodInfoAttributes,
            CliTokenSubstitutionPreprocessor preprocessor) : base(parameterInfo, methodInfo, methodInfoAttributes, preprocessor)
        {
            ArgumentMetadata = methodInfoAttributes
                .OfType<CliRouteArgumentAttribute>()
                .Where(arg => arg
                    .References(parameterInfo))
                .Do((args, count) =>
                {
                    if (count > 0)
                    {
                        throw new InvalidOperationException("Oops...");
                    }
                })
                .SingleOrDefault();

            if (CliOptionBundle.IsAssignableFrom(ParameterInfo.ParameterType))
            {
                throw new NotImplementedException();
            }
            else
            {
                
            }
        }

        public override object? Bind(Match commandLineMatch)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<JazzyOption> AsBundle()
        {
            throw new NotImplementedException();
        }


        public CliRouteArgumentAttribute? ArgumentMetadata { get; }

        public CliOptionAttribute? OptionMetadata { get; }

        

        public object? ExtractValue(Match commandLineMatch)
        {
            throw new NotImplementedException();
        }
    }

    sealed record OperandInfo
    {
        public OperandInfo(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
        }

        public OperandInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public  ParameterInfo? ParameterInfo { get; }
        public  PropertyInfo? PropertyInfo { get; }
    };


    private readonly ValueBinder[] _parameterValueBinders;
    private readonly OperandInfo[] _operands;




    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Func<object?[], Task<int>> _actionHandler;
    private readonly CliMasterOptionBundle[] _masterOptions;
    private readonly ICliActionSchema _schema;
    private readonly ICliCommandMethodParametersFactory _parametersFactory;

    public CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptionBundles,
        IEnumerable<CliRouteAttribute> baseRouteMetadata)
    {
        ThrowIf.ArgumentNull(method);
        ThrowIf.ArgumentNull(masterOptionBundles);
        ThrowIf.ArgumentNull(baseRouteMetadata);
        var methodAttributes = method.GetCustomAttributes().ToList();
        var parameters = method.GetParameters();

        var parameterValueBinders = new List<ValueBinder>();
        var operands = new List<OperandInfo>();
        foreach (var parameter in parameters)
        {
            var parameterAttributes = parameter.GetCustomAttributes().ToList();
            var parameterName = ThrowIf.NullOrWhiteSpace(parameter.Name);
            if (CliOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                throw new NotImplementedException();
            }

            var description = parameterAttributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .FirstOrDefault($"{method.Name} method parameter.");
            var option = parameterAttributes
                .OfType<CliOptionAttribute>()
                .SingleOrDefault() ?? new CliOptionAttribute($"--{parameterName.ToLowerInvariant()}", description);

            var optionArity = CliUtils.GetOptionArity(parameter.ParameterType);
            var underlyingType = CliUtils.GetUnderlyingType(parameter.ParameterType);
            var operand = new OperandInfo(parameter);
            operands.Add(operand);
        }
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


    public int Execute(string commandLine, CliTokenSubstitutionPreprocessor preProcessor)
    {
        commandLine = ThrowIf.ArgumentNullOrWhiteSpace(commandLine);
        var match = _schema.Match(
            commandLine, 
            preProcessor, 
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

        var args = _parametersFactory.BuildMethodArguments(match, preProcessor);

        foreach (var bundle in _masterOptions)
        {
            bundle.PopulateFrom(match, preProcessor);
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