using System.Text.RegularExpressions;

namespace Solitons.Postgres.PgUp;

public class PgUpScriptPreprocessor(IReadOnlyDictionary<string, string> parameters)
{
    public string Transform(string input)
    {
        foreach (var parameter in parameters)
        {
            var placeholder = $"${{{parameter.Key}}}";
            input = input.Replace(placeholder, parameter.Value, StringComparison.OrdinalIgnoreCase);
        }

        var regex = new Regex($@"\${{(\S+?)}}");
        var unresolvedParametersCsv = regex
            .Matches(input)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Join(", ");
        if (unresolvedParametersCsv.IsPrintable())
        {
            throw new InvalidOperationException(
                $"The following parameters could not be substituted: '{unresolvedParametersCsv}'. Please ensure they are defined in the pgup project file.");
        }
        return input;
    }
}