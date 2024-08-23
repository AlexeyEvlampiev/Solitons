using System;
using System.Text.RegularExpressions;

namespace Solitons.Text;

/// <summary>
/// Contains regular expression patterns for common use cases.
/// </summary>
public static partial class RegexPatterns
{
    /// <summary>
    /// Regular expression pattern for matching email addresses.
    /// </summary>
    public const string Email = @"^[a-zA-Z0-9.!#$%&''*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";


    /// <summary>
    /// Wraps the given regex pattern in a named or anonymous group if it is not already grouped.
    /// </summary>
    /// <param name="regexPattern">The regex pattern to wrap.</param>
    /// <param name="groupName">The name of the group. If null or whitespace, an anonymous group is used.</param>
    /// <returns>The wrapped regex pattern.</returns>
    /// <exception cref="ArgumentException">Thrown when the regexPattern is null or whitespace.</exception>
    public static string WrapWithRegexGroup(string regexPattern, string? groupName)
    {
        regexPattern = regexPattern
            .DefaultIfNullOrWhiteSpace("(?=.|$)")
            .Trim();


        ValidateRegexPattern(regexPattern);

        var anonymousGroup = "(?:";
        var namedGroupPrefix = "(?<";
        var groupStart = string.IsNullOrWhiteSpace(groupName) ? anonymousGroup : $"{namedGroupPrefix}{groupName}>";


        // Check if the pattern already starts with an anonymous group or a named group
        if ((regexPattern.StartsWith(anonymousGroup) || regexPattern.StartsWith(namedGroupPrefix)) && regexPattern.EndsWith(")"))
        {
            // If trying to wrap in an anonymous group and it is already a named group, return the pattern as is
            if (string.IsNullOrWhiteSpace(groupName) && regexPattern.StartsWith(namedGroupPrefix))
            {
                return regexPattern;
            }

            return regexPattern;
        }

        return $"{groupStart}{regexPattern})";
    }

    /// <summary>
    /// Validates that the provided pattern is a valid regular expression.
    /// </summary>
    /// <param name="pattern">The regex pattern to validate.</param>
    
    public static void ValidateRegexPattern(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Invalid regex pattern.", nameof(pattern), ex);
        }
    }


    /// <summary>
    /// Generates a regular expression pattern that matches one or more values from the specified enumeration type.
    /// </summary>
    /// <typeparam name="T">The type of the enumeration.</typeparam>
    /// <param name="values">The values to match.</param>
    /// <param name="ignoreCase">True to ignore case when matching.</param>
    /// <returns>A regular expression pattern that matches the specified values.</returns>
    public static string GenerateRegexExpression<T>(T values, bool ignoreCase = true) where T : Enum
    {
        var expression = values
            .ToString()
            .Replace(@"\s*,\s*", m => "|");
        var settings = ignoreCase ? "(?si)" : "(?s)";
        return $"{settings}^(?:{expression})$";
    }
}