namespace Solitons.CommandLine;

public interface ICliRouteArgumentMetadata
{
    /// <summary>
    /// Gets the name of the parameter associated with this attribute.
    /// </summary>
    string ParameterName { get; }

    /// <summary>
    /// Gets or sets a description of the parameter, which can be used to generate help text in CLI applications.
    /// </summary>
    string Description { get; init; }

    /// <summary>
    /// Gets or sets the role of the argument in the command, indicating its purpose or usage context.
    /// </summary>
    /// <remarks>
    /// The ArgumentRole property describes the functional role or semantic category of the CLI argument. 
    /// This description can help users understand what type of information should be provided when the command is used.
    /// For example, in a command like 'git clone [repository]', the ArgumentRole for 'repository' might be 'SourceRepository',
    /// indicating that the argument should be a URL or path to a git repository.
    /// </remarks>
    string ArgumentRole { get; init; }
}