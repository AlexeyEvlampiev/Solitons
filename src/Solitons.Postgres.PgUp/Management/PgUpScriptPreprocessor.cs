using System.Text.RegularExpressions;

namespace Solitons.Postgres.PgUp.Management;

public class PgUpScriptPreprocessor(IReadOnlyDictionary<string, string> parameters)
{
    public static string Transform(
        string input,
        IReadOnlyDictionary<string, string> parameters)
    {
        return Regex.Replace(input, @"\${([^\s{}]+)}", m =>
        {
            var key = m.Groups[1].Value;
            if (parameters.TryGetValue(key, out var value))
            {
                return value;
            }

            return m.Value;
        });
    }
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
            .Join(", ");
        if (unresolvedParametersCsv.IsPrintable())
        {
            throw new NotImplementedException();
        }
        return input;
    }
}