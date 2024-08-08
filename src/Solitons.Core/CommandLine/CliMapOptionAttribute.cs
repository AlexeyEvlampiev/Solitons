using System;

namespace Solitons.CommandLine;

public sealed class CliMapOptionAttribute : CliOptionAttribute
{

    public CliMapOptionAttribute(
        string specification, 
        string description = "", 
        StringComparison comparison = StringComparison.OrdinalIgnoreCase) 
        : base(specification, description)
    {
        this.Comparer = comparison.ToStringComparer();
    }

    public StringComparer Comparer { get; }
}


