using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Solitons.Data;

public sealed class DbCommandTextHasher : IDisposable
{
    private readonly SHA256 _crypto;
    private readonly Regex _regex;
    private int _disposed = 0;

    public DbCommandTextHasher()
    {
        _crypto = SHA256.Create();
        var pattern = @"(?xis-m)(?:@text|@comment|\s+)"
            .Replace("@text", @"(?<text>""[^""]*"")|(?<text>'[^']*')")
            .Replace("@comment", @"--[^\n]*\n|/[*].*?[*]/");
        _regex = new Regex(pattern);
    }

    public string ComputeHash(string commandText)
    {
        if (Thread.VolatileRead(ref _disposed) == 1)
        {
            throw new ObjectDisposedException(
                nameof(DbCommandTextHasher), 
                $"The {typeof(DbCommandTextHasher)} instance has been disposed and cannot be used.");
        }
        commandText = _regex.Replace(commandText, m =>
        {
            if (m.Groups["text"].Success)
            {
                return m.Value;
            }

            return string.Empty;
        });
        var hashHex = _crypto
            .ComputeHash(Encoding.UTF8.GetBytes(commandText))
            .Select(b => b.ToString("x2"))
            .Join(string.Empty);
        return hashHex;
    }

    void IDisposable.Dispose()
    {
        if (0 == Interlocked.CompareExchange(ref _disposed, 1, 0))
        {
            _crypto.Dispose();
        }
    }
}