using System;
using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine;

internal interface ICliRouteArgumentSegmentMetadata : ICliRouteSegmentMetadata
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

    string ICliRouteSegmentMetadata.BuildRegularExpression(
        IReadOnlyList<ICliRouteSegmentMetadata> segments)
    {
        var index = segments.IndexOf(this);
        if (index < 0)
        {
            throw new ArgumentException("Oops...");
        }

        var lookAhead = segments
            .OfType<ICliRouteCommandSegmentMetadata>()
            .Select(cmd => cmd.BuildRegularExpression())
            .Concat([@"(?:[^\s\-])"])
            .Join("|")
            .Convert(p => $"(?!(?:{p}))");

        var lookBehind = segments
            .Select(segment =>
            {
                if (segment is ICliRouteArgumentSegmentMetadata)
                {
                    return $@"[^\s\-]\S+";
                }

                if (segment is ICliRouteCommandSegmentMetadata cmd)
                {
                    return cmd.BuildRegularExpression();
                }

                throw new InvalidOperationException("Oops...");
            })
            .Join(@"\s+")
            .Convert(p => $"(?<=(?:{p}))");



        var pattern = segments
            .Take(index)
            .Select(segment =>
            {
                if (segment is ICliRouteCommandSegmentMetadata cmd)
                {
                    return cmd.BuildRegularExpression();
                }

                if (segment is ICliRouteArgumentSegmentMetadata arg)
                {
                    return @$"{lookAhead}{lookBehind}\S+";
                }

                throw new InvalidOperationException("Oops...");
            })
            .Join(@"\s+")
            .Convert(p => $"(?:{p})");

        return pattern;
    }
}