using System.Text.RegularExpressions;

namespace Solitons.Postgres.PgUp.Management;

public class PgUpScriptPreprocessor(IReadOnlyDictionary<string, string> parameters)
{
    public string Convert(string input)
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