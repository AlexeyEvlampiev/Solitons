using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Globalization;
using Solitons.CommandLine.ZapCli;

namespace Solitons.CommandLine;

internal abstract class CliOperandInfo : IFormattable
{
    private readonly TypeConverter _typeConverter;
    private readonly MetadataCollection _metadata = new();
    private readonly object _attribute;

    protected CliOperandInfo(
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
            Name = NormalizeName(parameter.Name);
            ParameterType = parameter.ParameterType;
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


        if (CliMasterOptionBundle.IsAssignableFrom(ParameterType))
        {
            throw new InvalidOperationException();
        }

        _attribute = _metadata
            .Where(m => m is CliOptionAttribute || m is CliArgumentAttribute)
            .FirstOrDefault(new CliOptionAttribute($"--{Name}".ToLower(), _metadata.Descriptions.FirstOrDefault(Name)));


        RegularExpression = _metadata
            .OfType<CliOptionAttribute>()
            .Select(o => o.OptionSpecification)
            .FirstOrDefault("^");

        Description = _metadata.Descriptions.FirstOrDefault(Name);


        Cardinality = ZapCliParameterCardinality.Scalar;

        UnderlyingType = ParameterType!;
        if (UnderlyingType.IsValueType)
        {
            UnderlyingType = Nullable.GetUnderlyingType(UnderlyingType) ?? UnderlyingType;
            Cardinality = ZapCliParameterCardinality.Scalar;
            if (UnderlyingType == typeof(Unit))
            {
                Cardinality = ZapCliParameterCardinality.Flag;
            }
        }

        _typeConverter = CustomAttributes
            .OfType<TypeConverterAttribute>()
            .Select(a => Type
                .GetType(a.ConverterTypeName)
                .Convert(type => ThrowIf.NullReference(type, $""))
                .Convert(type => (TypeConverter?)Activator.CreateInstance(type))
                .Convert(converter => ThrowIf.NullReference(converter, $"")))
            .SingleOrDefault(TypeDescriptor.GetConverter(UnderlyingType));

        if (UnderlyingType.IsEnum)
        {
            _typeConverter = new EnumConverter(UnderlyingType);
        }

        if (Cardinality != ZapCliParameterCardinality.Flag &&
            false == _typeConverter.CanConvertFrom(typeof(string)))
        {
            throw new InvalidOperationException();
        }
    }


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

    public string RegularExpression { get; }

    public string NamedGroupPattern
    {
        get
        {
            if (Cardinality == ZapCliParameterCardinality.Flag)
            {
                return $"(?<{Name}>(?:{RegularExpression}))";
            }

            return $@"(?:(?:{RegularExpression})\s+(?<{Name}>\S*))";
        }
    }

    public Type ParameterType { get; }

    public bool IsOptional { get; }

    protected bool IsArgument => _metadata.Arguments.Any();

    internal Type UnderlyingType { get; }

    internal ZapCliParameterCardinality Cardinality { get; }

    public virtual object? Convert(string token)
    {
        return _typeConverter.ConvertFromString(token);
    }





    public override string ToString() => ToString("G", CultureInfo.CurrentCulture);


    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format = format?
            .DefaultIfNullOrEmpty("G");

        switch (format?.ToUpperInvariant())
        {
            case "G": return Name;
            case "D": return Description;
            case "H":
                {
                    if (_attribute is CliArgumentAttribute arg)
                    {
                        return $"<{arg.ArgumentRole}>".ToUpper();
                    }
                    else if (_attribute is CliOptionAttribute options)
                    {
                        return options.OptionSpecification;
                    }

                    throw new InvalidOperationException();
                }
            default:
                throw new FormatException($"The format string '{format}' is not supported.");
        }
    }


    protected bool FindValue(Match match, out object? value)
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
            if (captures.Count > 1)
            {
                throw new NotImplementedException();
            }

            if (Cardinality == ZapCliParameterCardinality.Flag)
            {
                value = Unit.Default;
                return true;
            }
            value = Convert(group.Value);
            return true;
        }


        if (IsOptional)
        {
            return false;
        }

        throw new NotImplementedException();
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