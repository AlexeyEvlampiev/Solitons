namespace Solitons.CommandLine.ZapCli;

/// <summary>
/// Enumerates the cardinality of command line parameters, indicating how many values are expected for each type.
/// </summary>
public enum ZapCliParameterCardinality
{
    /// <summary>
    /// A flag parameter, which acts as a toggle and does not require accompanying values.
    /// </summary>
    Flag,

    /// <summary>
    /// A scalar parameter, which requires exactly one value, typically used to specify a discrete setting or argument.
    /// </summary>
    Scalar,

    /// <summary>
    /// A collection parameter, which can accept multiple values, useful for specifying varied inputs or multiple entries.
    /// </summary>
    Collection,

    /// <summary>
    /// A map parameter, which involves pairs of keys and values, ideal for complex configurations that require structured data.
    /// </summary>
    Map
}