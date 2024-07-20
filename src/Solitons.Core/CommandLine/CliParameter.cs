using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Solitons.CommandLine;

public abstract class CliParameter : CliOptionAttribute
{
    [DebuggerStepThrough]
    protected internal CliParameter(
        string capture, 
        Type parameterType, 
        string options, 
        string description) 
        : base(options, description)
    {
        Capture = capture;
        ParameterType = parameterType;
    }

    public string Capture { get; }

    public Type ParameterType { get; }

    public static bool IsRequired<T>() where T : CliParameter => typeof(T).GetConstructor([]) is not null;

    public static bool IsAssignableFrom(Type type) => typeof(CliParameter).IsAssignableFrom(type);

    public static bool IsAssignableFrom(PropertyInfo pi) => typeof(CliParameter).IsAssignableFrom(pi.PropertyType);

    public static bool IsAssignableFrom(ParameterInfo pi) => typeof(CliParameter).IsAssignableFrom(pi.ParameterType);
}

public abstract class CliParameter<T> : CliParameter
{
    private Func<T> _getOrParse;

    [DebuggerStepThrough]
    protected CliParameter(string capture, string options, string description) 
        : base(capture, typeof(T), options, description)
    {
        var lazy = new Lazy<T>(Parse);
        _getOrParse = () => lazy.Value;
        T Parse()
        {
            if (TryParse(out var value))
            {
                return value!;
            }

            throw new FormatException();
        }
    }

    protected virtual bool TryParse(out T? value)
    {
        var converter = TypeDescriptor.GetConverter(this.ParameterType);
        object? result = converter.ConvertFromInvariantString(Capture);
        if (result is null)
        {
            value = default;
            return false;
        }

        value = (T)result;
        return true;
    }


    public static implicit operator T(CliParameter<T> parameter) => parameter._getOrParse.Invoke() ?? throw new NullReferenceException();

}