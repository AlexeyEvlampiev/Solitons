using System.ComponentModel;

namespace Solitons.CommandLine;

internal static class Extensions
{
    /// <summary>
    /// Determines whether the specified <see cref="TypeConverter"/> can 
    /// convert a CLI operand, specifically if it can handle string input.
    /// </summary>
    /// <param name="converter">The <see cref="TypeConverter"/> to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the converter is a <see cref="StringConverter"/> 
    /// or can convert from a <see cref="string"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool SupportsCliOperandConversion(this TypeConverter converter) =>
        converter is StringConverter || 
        converter.CanConvertFrom(typeof(string));
}