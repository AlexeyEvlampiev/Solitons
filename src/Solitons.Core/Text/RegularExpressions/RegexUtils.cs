using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Solitons.Text.RegularExpressions;

public static class RegexUtils
{
    private static readonly Regex RemoveWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex ValidGroupNameRegex = new(@"^\w+$", RegexOptions.Compiled);

    /// <summary>
    /// Ensures that the given pattern is wrapped in a non-capturing group, if it is not already grouped.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to ensure grouping.</param>
    /// <returns>The pattern wrapped in a non-capturing group if it wasn't grouped.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="pattern"/> is null or whitespace.</exception>
    public static string EnsureNonCapturingGroup(string pattern)
    {
        pattern = ThrowIf.ArgumentNullOrWhiteSpace(pattern).Trim();
        return pattern.StartsWith("(")
            ? pattern
            : $"(?:{pattern})";
    }

    /// <summary>
    /// Ensures that the given pattern is wrapped in a named group, if it is not already wrapped.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to ensure grouping.</param>
    /// <param name="groupName">The name for the capturing group.</param>
    /// <returns>The pattern wrapped in a named capturing group if it wasn't already grouped with the specified name.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="pattern"/> or <paramref name="groupName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="groupName"/> is not a valid group name (contains non-alphanumeric characters).</exception>
    public static string EnsureNamedGroup(string pattern, string groupName)
    {
        pattern = ThrowIf.ArgumentNullOrWhiteSpace(pattern).Trim();
        groupName = ThrowIf.ArgumentNullOrWhiteSpace(groupName).Trim();

        // Validate groupName to contain only word characters
        if (!ValidGroupNameRegex.IsMatch(groupName))
        {
            throw new ArgumentException("Group name must consist of alphanumeric characters and underscores.", nameof(groupName));
        }

        // Check if the pattern is already grouped with the specific group name
        if (Regex.IsMatch(pattern, $@"^\(\?<{groupName}>"))
        {
            return pattern;
        }

        // Check if the pattern is already in a non-capturing group
        if (pattern.StartsWith("(?:"))
        {
            return Regex.Replace(pattern, @"^\(\?:", $"(?<{groupName}>");
        }

        // Otherwise, wrap the pattern in the named capturing group
        return $"(?<{groupName}>{pattern})";
    }


    /// <summary>
    /// Removes all whitespace from the input string.
    /// </summary>
    /// <param name="input">The input string from which whitespace should be removed.</param>
    /// <returns>The input string with all whitespace characters removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="input"/> is null.</exception>
    [DebuggerNonUserCode]
    public static string RemoveWhitespace(string input) =>
        RemoveWhitespaceRegex.Replace(input, string.Empty);
}
