using System.IO.Compression;
using System.Text.RegularExpressions;
using Solitons.CommandLine;
using Solitons.Data;
using Solitons.Postgres.PgUp.Models;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpBatch
{
    private readonly DirectoryInfo _batchWorkingDirectory;
    private readonly byte[] _scripts;


    public sealed record Script(string RelativePath, string Content, string Checksum)
    {
        public void Write(BinaryWriter writer)
        {
            writer.Write(RelativePath);
            writer.Write(Content);
            writer.Write(Checksum);
        }

        public static Script Read(BinaryReader reader)
        {
            var relativePath = reader.ReadString();
            var content = reader.ReadString();
            var checksum = reader.ReadString();
            return new Script(relativePath, content, checksum);
        }
    }

    public PgUpBatch(
        IPgUpBatch batch,
        DirectoryInfo pgUpWorkingDirectory,
        PgUpScriptPreprocessor preProcessor)
    {
        ThrowIf.ArgumentNull(batch);
        ThrowIf.ArgumentNull(pgUpWorkingDirectory);
        ThrowIf.ArgumentNull(preProcessor);

        _batchWorkingDirectory = new DirectoryInfo(Path.Combine(pgUpWorkingDirectory.FullName, batch.GetWorkingDirectory()));
        if (false == _batchWorkingDirectory.Exists)
        {
            throw new CliExitException(
                $"Specified pgup working directory not found. Directory: '{_batchWorkingDirectory.FullName}'");
        }

        CustomExecCommand = batch.GetCustomExecCommandText();

        string[] scriptFiles = batch.GetFileDiscoveryMode() switch
        {
            FileDiscoveryMode.MatchOnly => DiscoverFilesByRunOrder(FindSqlFiles(SearchOption.AllDirectories), batch.GetRunOrder()),
            FileDiscoveryMode.ShallowDiscovery => DiscoverFilesShallow(batch.GetRunOrder()),
            FileDiscoveryMode.DeepDiscovery => DiscoverFilesDeep(batch.GetRunOrder()),
            _ => throw new InvalidOperationException("Invalid file discovery mode.")
        };



        using var memory = new MemoryStream();
        using var zipStream = new GZipStream(memory, CompressionLevel.SmallestSize);
        using var writer = new BinaryWriter(zipStream);
        using var alg = new DbCommandTextHasher();
        writer.Write(scriptFiles.Length);
        foreach (var scriptFullName in scriptFiles)
        {
            if (false == File.Exists(scriptFullName))
            {
                throw new CliExitException($"SQL script not found: '{scriptFullName}'. Verify path and filename.");
            }

            var content = File.ReadAllText(scriptFullName);
            var checksum = alg.ComputeHash(content);

            content = preProcessor.Transform(content);
            var relativePath = Path
                .GetRelativePath(pgUpWorkingDirectory.FullName, scriptFullName)
                .Convert(p => Regex.Replace(p, @"\\", "/"));

            var script = new Script(relativePath, content, checksum);
            script.Write(writer);
        }
        zipStream.Flush();
        writer.Flush();
        memory.Position = 0;
        _scripts = memory.ToArray();
    }



    public string? CustomExecCommand { get; }

    public IEnumerable<Script> GetScripts()
    {
        using var memory = new MemoryStream(_scripts);
        using var zipStream = new GZipStream(memory, CompressionMode.Decompress);
        using var reader = new BinaryReader(zipStream);
        int scriptCount = reader.ReadInt32();
        var scripts = new List<Script>(scriptCount);
        for (int i = 0; i < scriptCount; i++)
        {
            var script = Script.Read(reader);
            scripts.Add(script);
        }
        return scripts;
    }


    private string[] FindSqlFiles(SearchOption searchOption)
    {
        return _batchWorkingDirectory
            .GetFiles("*.sql", searchOption)
            .Select(f => f.FullName.Replace("\\", "/"))
            .ToArray();
    }

    private string[] DiscoverFilesByRunOrder(string[] allFiles, IEnumerable<string> runOrder)
    {
        var orderedFiles = runOrder
            .SelectMany(pattern =>
        {
            if (File.Exists(Path.Combine(_batchWorkingDirectory.FullName, pattern)))
            {
                return [Path.Combine(_batchWorkingDirectory.FullName, pattern)];
            }

            try
            {
                var regex = new Regex(pattern.Replace("\\", "/"));
                return allFiles.Where(path => regex.IsMatch(path)).ToArray();
            }
            catch (ArgumentException ex)
            {
                throw new CliExitException(
                    $"Invalid runOrder file pattern defined for the '{_batchWorkingDirectory}' directory. '{pattern}': {ex.Message}");
            }
        }).ToList();

        var remainingFiles = allFiles.Except(orderedFiles)
            .OrderBy(f => f)
            .ToArray();

        return orderedFiles.Concat(remainingFiles).ToArray();
    }

    private string[] DiscoverFilesShallow(IEnumerable<string> runOrder)
    {
        // Find all .sql files in the top directory only
        var files = FindSqlFiles(SearchOption.TopDirectoryOnly);

        // Order the files based on the provided runOrder patterns
        var orderedFiles = files
            .OrderBy(f =>
            {
                foreach (var pattern in runOrder)
                {
                    if (Regex.IsMatch(f, pattern))
                    {
                        return 0; // Matches a pattern in runOrder, so prioritize it
                    }
                }
                return 1; // No match found in runOrder, so deprioritize
            })
            .ThenBy(f => f) // Secondary sorting alphabetically
            .ToArray();

        // Further refine the order using the DiscoverFilesByRunOrder method
        return DiscoverFilesByRunOrder(orderedFiles, runOrder);
    }


    private string[] DiscoverFilesDeep(IEnumerable<string> runOrder)
    {
        // Find all .sql files in all directories (including nested ones)
        var files = FindSqlFiles(SearchOption.AllDirectories);

        // Order the files based on the provided runOrder patterns
        var orderedFiles = files
            .OrderBy(f =>
            {
                foreach (var pattern in runOrder)
                {
                    if (Regex.IsMatch(f, pattern))
                    {
                        return 0; // Matches a pattern in runOrder, so prioritize it
                    }
                }
                return 1; // No match found in runOrder, so deprioritize
            })
            .ThenBy(f => f) // Secondary sorting alphabetically
            .ToArray();

        // Further refine the order using the DiscoverFilesByRunOrder method
        return DiscoverFilesByRunOrder(orderedFiles, runOrder);
    }

}

