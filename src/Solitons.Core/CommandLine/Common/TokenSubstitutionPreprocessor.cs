using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.Common;

public sealed class TokenSubstitutionPreprocessor
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly FrozenDictionary<string,string> _substitutions;

    private TokenSubstitutionPreprocessor(Dictionary<string, string> substitutions)
    {
        _substitutions = substitutions.ToFrozenDictionary();
    }

    public static string SubstituteTokens(string commandLine, out TokenSubstitutionPreprocessor tokenSubstitutionPreprocessor)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        tokenSubstitutionPreprocessor = new TokenSubstitutionPreprocessor(dictionary);
        return Regex.Replace(
            commandLine, 
        @"""([^""]*)""", 
        match =>
            {
                var group = match.Groups[1];
                Debug.Assert(group.Success);
                var key = Guid.NewGuid().ToString("N");
                dictionary[key] = group.Value;
                return key;
            });
    }

    public string GetSubstitution(string key) => _substitutions.GetValueOrDefault(key, key);
}