using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;

namespace Solitons.CommandLine;

internal abstract class CliOperandInfo
{
    private readonly MetadataCollection _metadata = new();

    protected CliOperandInfo(ParameterInfo parameter) 
        : this((ICustomAttributeProvider)parameter)
    {
        Name = NormalizeName(parameter.Name);
        ParameterType = parameter.ParameterType;
    }

    protected CliOperandInfo(PropertyInfo source)
        : this((ICustomAttributeProvider)source)
    {

    }

    private CliOperandInfo(
        ICustomAttributeProvider source)
    {
        using var ctorCallback = Disposable.Create(Validate);

        CustomAttributes = source.GetCustomAttributes(true);
        _metadata.AddRange(
            CustomAttributes
                .OfType<CliOptionAttribute>());
        _metadata.AddRange(
            CustomAttributes
                .OfType<DescriptionAttribute>());

        var annotatedAsRequired = CustomAttributes.OfType<RequiredAttribute>().Any();



        if (source is ParameterInfo parameter)
        {
            IsOptional = parameter.IsOptional &&
                         false == annotatedAsRequired;
            _metadata.AddRange(parameter
                .Member
                .GetCustomAttributes()
                .OfType<CliArgumentAttribute>()
                .Where(argument => argument.References(parameter)));
        }
        else if (source is PropertyInfo propertyInfo)
        {
            Name = NormalizeName(propertyInfo.Name);
            ParameterType = propertyInfo.PropertyType;
            IsOptional = false == annotatedAsRequired;
        }
        else
        {
            throw new InvalidOperationException();
        }

        Aliases = CustomAttributes
            .OfType<CliOptionAttribute>()
            .Select(a => a.Aliases)
            .FirstOrDefault([]);

        var underlyingParameterType = Nullable.GetUnderlyingType(ParameterType) ?? ParameterType;
        OperandArity = underlyingParameterType switch
        {
            // Check if the type implements IDictionary<string, T>
            { } t when t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                    i.GetGenericArguments()[0] == typeof(string))
                => CliOperandArity.Map,

            // Check if the type implements IEnumerable but is not a string
            { } t when typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string)
                => CliOperandArity.Vector,

            // Check if the type represents a Unit (assuming Unit is a specific type in your code)
            { } t when t == typeof(Unit)
                => CliOperandArity.Flag,

            // Default case: Scalar
            _ => CliOperandArity.Scalar,
        };




        if (CliMasterOptionBundle.IsAssignableFrom(ParameterType))
        {
            throw new InvalidOperationException();
        }

    

        OperandKeyPattern = _metadata
            .OfType<CliOptionAttribute>()
            .Select(o => o.OptionSpecification)
            .Select(s => Regex.Replace(s, @"\?", @"\?"))
            .FirstOrDefault("^");

        Description = _metadata.Descriptions.FirstOrDefault(Name);

        var customTypeConverter = CustomAttributes
            .OfType<TypeConverterAttribute>()
            .Select(att => Type.GetType(att.ConverterTypeName))
            .Where(type => type != null)
            .Select(Activator.CreateInstance!)
            .OfType<TypeConverter>()
            .FirstOrDefault() ?? CustomAttributes
            .OfType<CliOptionAttribute>()
            .Select(a => a.GetCustomTypeConverter())
            .FirstOrDefault(c => c is not null);

        if (ParameterType == typeof(CancellationToken))
        {
            customTypeConverter ??= new CliCancellationTokenTypeConverter();
        }
        else if (ParameterType == typeof(TimeSpan) || 
                 ParameterType == typeof(TimeSpan?))
        {
            customTypeConverter ??= new MultiFormatTimeSpanConverter();
        }
        Converter = CliOperandTypeConverter.Create(ParameterType, Name, Metadata, customTypeConverter);
    }


    public CliOperandArity OperandArity { get; }

    public IReadOnlyList<string> Aliases { get; }

    private void Validate()
    {
        var arguments = _metadata.Arguments.ToList();
        var options = _metadata.Options.ToList();
        if (arguments.Count > 1)
        {
            throw new InvalidOperationException();
        }

        if (arguments.Any() &&
            options.Any())
        {
            throw new InvalidOperationException("Or that or that");
        }
    }

    public object[] CustomAttributes { get; }

    internal IReadOnlyList<object> Metadata => _metadata;

    public string Name { get; }

    public string Description { get; protected set; }

    public string OperandKeyPattern { get; }

    public Type ParameterType { get; }

    public bool IsOptional { get; }


    internal CliOperandTypeConverter Converter { get; }

    public override string ToString() => Name;


    protected bool FindValue(Match match, CliTokenSubstitutionPreprocessor preprocessor, out object? value)
    {
        value = false;
        if (false == match.Success)
        {
            throw new ArgumentException();
        }
        var group = match.Groups[Name];
        if (group.Success)
        {
            var captures = group.Captures;
            if (captures.Count > 1 && Converter.AllowsMultipleValues == false)
            {
                throw new NotImplementedException();
            }

            value = Converter.FromMatch(match, preprocessor);
            return true;
        }


        if (IsOptional)
        {
            return false;
        }

        Debug.WriteLine($"{Name}");
        throw new CliExitException($"{this.Name} parameter is required.");
    }


    private static string NormalizeName(string? name)
    {
        return ThrowIf
            .NullOrWhiteSpace(name)
            .Convert(s => Regex.Replace(s, @"^\W+", string.Empty))
            .Trim();

    }

    protected sealed class MetadataCollection : Collection<object>
    {
        [DebuggerStepThrough]
        public void AddRange(IEnumerable<object> range) => range.ForEach(Add);

        [DebuggerStepThrough]
        protected override void InsertItem(int index, object item)
        {
            Validate(item);
            base.InsertItem(index, item);
        }

        [DebuggerStepThrough]
        protected override void SetItem(int index, object item)
        {
            Validate(item);
            base.SetItem(index, item);
        }

        public IEnumerable<CliArgumentAttribute> Arguments => this.OfType<CliArgumentAttribute>();

        public IEnumerable<CliOptionAttribute> Options => this.OfType<CliOptionAttribute>();


        public IEnumerable<string> Descriptions => this
            .OfType<CliArgumentAttribute>()
            .Select(_ => _.Description)
            .Union(this.OfType<CliOptionAttribute>()
                .Select(attribute => attribute.Description))
            .Union(this.OfType<DescriptionAttribute>()
                .Select(attribute => attribute.Description));


        private void Validate(object item)
        {
            if (item is CliArgumentAttribute argument)
            {
                if (Arguments.Any(a => a.Conflicts(argument)))
                {
                    throw new InvalidOperationException($"Multiple cli argument declarations referencing the {argument.ParameterName} parameter");
                }
            }
        }

    }
}