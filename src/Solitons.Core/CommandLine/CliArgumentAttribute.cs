using System;
using System.ComponentModel;
using System.Reflection;

namespace Solitons.CommandLine;

/// <summary>
/// Attribute to reference a parameter of the target method, describing its purpose and usage in CLI commands.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CliRouteArgumentAttribute : Attribute, ICliRouteArgumentMetadata
{
    /// <summary>
    /// Initializes a new instance of the CliArgumentAttribute class.
    /// </summary>
    /// <param name="parameterName">The name of the parameter this attribute is associated with.</param>
    /// <param name="description">A description of what the parameter is used for.</param>
    public CliRouteArgumentAttribute(
        string parameterName, 
        string description)
    {
        ParameterName = ThrowIf
            .ArgumentNullOrWhiteSpace(parameterName)
            .Trim(); 
        Description = description
            .DefaultIfNullOrWhiteSpace(ParameterName);
        ArgumentRole = ParameterName;
    }

    /// <summary>
    /// Gets the name of the parameter associated with this attribute.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets or sets a description of the parameter, which can be used to generate help text in CLI applications.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Gets or sets the role of the argument in the command, indicating its purpose or usage context.
    /// </summary>
    /// <remarks>
    /// The ArgumentRole property describes the functional role or semantic category of the CLI argument. 
    /// This description can help users understand what type of information should be provided when the command is used.
    /// For example, in a command like 'git clone [repository]', the ArgumentRole for 'repository' might be 'SourceRepository',
    /// indicating that the argument should be a URL or path to a git repository.
    /// </remarks>
    public string ArgumentRole { get; init; }

    /// <summary>
    /// Determines if this attribute conflicts with another by comparing their parameter names.
    /// </summary>
    /// <param name="other">Another CliArgumentAttribute to compare against.</param>
    /// <returns>True if both attributes refer to the same parameter name; otherwise, false.</returns>
    public bool Conflicts(CliRouteArgumentAttribute other) => ParameterName
        .Equals(other.ParameterName, StringComparison.Ordinal);


    /// <summary>
    /// Checks if this attribute references a specific parameter.
    /// </summary>
    /// <param name="pi">The ParameterInfo to check against.</param>
    /// <returns>True if this attribute's parameter name matches the provided ParameterInfo's name; otherwise, false.</returns>
    public bool References(ParameterInfo pi) => ParameterName.Equals(pi.Name);

    public virtual TypeConverter? GetCustomTypeConverter(out string inputSample)
    {
        inputSample = String.Empty;
        return null;
    }

    /// <summary>
    /// Returns a string that represents the parameter name.
    /// </summary>
    /// <returns>A string representation of the parameter name.</returns>
    public override string ToString() => ParameterName;

}