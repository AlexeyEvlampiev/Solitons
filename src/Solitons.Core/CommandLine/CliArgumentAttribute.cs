using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace Solitons.CommandLine;

/// <summary>
/// Attribute to reference a parameter of the target method, describing its purpose and usage in CLI commands.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CliArgumentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the CliArgumentAttribute class.
    /// </summary>
    /// <param name="parameterName">The name of the parameter this attribute is associated with.</param>
    /// <param name="description">A description of what the parameter is used for.</param>
    public CliArgumentAttribute(
        string parameterName, 
        string description)
    {
        ParameterName = ThrowIf
            .ArgumentNullOrWhiteSpace(parameterName)
            .Trim(); 
        Description = description
            .DefaultIfNullOrWhiteSpace(ParameterName);
        Name = ParameterName;
    }

    /// <summary>
    /// Gets the name of the parameter associated with this attribute.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets or sets a description of the parameter, which can be used to generate help text in CLI applications.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the role of the argument in the command, indicating its purpose or usage context.
    /// </summary>
    /// <remarks>
    /// The ArgumentRole property describes the functional role or semantic category of the CLI argument. 
    /// This description can help users understand what type of information should be provided when the command is used.
    /// For example, in a command like 'git clone [repository]', the ArgumentRole for 'repository' might be 'SourceRepository',
    /// indicating that the argument should be a URL or path to a git repository.
    /// </remarks>
    public string Name { get; init; }

    /// <summary>
    /// Determines if this attribute conflicts with another by comparing their parameter names.
    /// </summary>
    /// <param name="other">Another CliArgumentAttribute to compare against.</param>
    /// <returns>True if both attributes refer to the same parameter name; otherwise, false.</returns>
    public bool Conflicts(CliArgumentAttribute other) => ParameterName
        .Equals(other.ParameterName, StringComparison.Ordinal);


    /// <summary>
    /// Checks if this attribute references a specific parameter.
    /// </summary>
    /// <param name="pi">The ParameterInfo to check against.</param>
    /// <returns>True if this attribute's parameter name matches the provided ParameterInfo's name; otherwise, false.</returns>
    public bool References(ParameterInfo pi) => ParameterName.Equals(pi.Name);

    public virtual bool CanAccept(Type argumentType, out TypeConverter converter)
    {
        converter = TypeDescriptor.GetConverter(argumentType);

        if (argumentType == typeof(TimeSpan))
        {
            converter = new MultiFormatTimeSpanConverter();
            ThrowIf.False(converter.CanConvertFrom(typeof(string)));
        }
        else if (argumentType == typeof(CancellationToken))
        {
            converter = new CliCancellationTokenTypeConverter();
            ThrowIf.False(converter.CanConvertFrom(typeof(string)));
        }
        return converter.SupportsCliOperandConversion();
    }

    /// <summary>
    /// Returns a string that represents the parameter name.
    /// </summary>
    /// <returns>A string representation of the parameter name.</returns>
    public override string ToString() => ParameterName;

}