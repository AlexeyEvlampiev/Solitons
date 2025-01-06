using System.IO.Compression;
using System.Text.RegularExpressions;
using Solitons.Data;
using Solitons.Postgres.PgUp.Core.Models;

namespace Solitons.Postgres.PgUp.Core;

public sealed class PgUpBatch
{
    private delegate bool FileMatchPredicate(string fileFullName);
    private readonly DirectoryInfo _batchWorkingDirectory;
    private readonly byte[] _compressedScripts;
    private readonly string[] _fileOrderPatterns;


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
        _fileOrderPatterns = batch.GetRunOrder().ToArray();

        _batchWorkingDirectory = new DirectoryInfo(Path.Combine(pgUpWorkingDirectory.FullName, batch.GetWorkingDirectory()));
        if (false == _batchWorkingDirectory.Exists)
        {
            Console.WriteLine("Oops...");
            foreach (var info in pgUpWorkingDirectory.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                Console.WriteLine(info.FullName);
            }
            throw new PgUpExitException(
                $"The pgup batch working directory '{_batchWorkingDirectory.FullName}' was not found. " +
                $"Please ensure the directory exists and try again.");
        }

        CustomExecCommand = batch.GetCustomExecCommandText();


        FileMatchPredicate[] fileMatchers = batch
            .GetRunOrder()
            .Select(pattern =>
            {
                if (File.Exists(Path.Combine(_batchWorkingDirectory.FullName, pattern)))
                {

                    var fi = new FileInfo(Path.Combine(_batchWorkingDirectory.FullName, pattern));
                    var requiredFileFullName = fi.FullName.Replace("\\", "/");
                    return IsMatch;
                    bool IsMatch(string fileFullName)
                    {
                        var result = requiredFileFullName.Equals(fileFullName, StringComparison.OrdinalIgnoreCase);
                        return result;
                    }

                }

                try
                {
                    var regex = new Regex(pattern.Replace("\\", "/"), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return new FileMatchPredicate(IsMatch);
                    bool IsMatch(string fileFullName) => regex.IsMatch(fileFullName);
                }
                catch (ArgumentException ex)
                {
                    throw new PgUpExitException(
                        $"The file pattern '{pattern}' in the directory '{_batchWorkingDirectory.FullName}' is invalid. " +
                        $"{ex.Message} Please correct the pattern and try again.");
                    throw;
                }
            })
            .ToArray();

        string[] scriptFiles = batch.GetFileDiscoveryMode() switch
        {
            PgUpScriptDiscoveryMode.None => GetFilesInRunOrder(fileMatchers),
            PgUpScriptDiscoveryMode.Shallow => DiscoverFiles(fileMatchers, SearchOption.TopDirectoryOnly),
            PgUpScriptDiscoveryMode.Recursive => DiscoverFiles(fileMatchers, SearchOption.AllDirectories),
            _ => throw new InvalidOperationException(
                "The specified file discovery mode is not valid.")
        };



        using var memory = new MemoryStream();
        using var zipStream = new GZipStream(memory, CompressionLevel.SmallestSize);
        using var writer = new BinaryWriter(zipStream);
        using var alg = new SqlCommandTextHasher();
        writer.Write(scriptFiles.Length);
        foreach (var scriptFullName in scriptFiles)
        {
            if (false == File.Exists(scriptFullName))
            {
                throw new PgUpExitException(
                    $"The SQL script '{scriptFullName}' could not be found. " +
                    $"Verify the file path and name, then try again.");

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
        _compressedScripts = memory.ToArray();
    }



    public string? CustomExecCommand { get; }

    public IEnumerable<Script> GetScripts()
    {
        using var memory = new MemoryStream(_compressedScripts);
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


    private HashSet<string> GetSqlFiles(SearchOption searchOption)
    {
        return _batchWorkingDirectory
            .EnumerateFiles("*.sql", searchOption)
            .Select(f => f.FullName.Replace("\\", "/"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private string[] GetFilesInRunOrder(FileMatchPredicate[] runOrder)
    {
        var allSqlFiles = GetSqlFiles(SearchOption.AllDirectories);
        var matchedFiles = runOrder
            .SelectMany((isMatch, index) =>
            {
                var matches = allSqlFiles
                    .Where(fileFullName => isMatch(fileFullName))
                    .ToList();
                if (matches.Any() == false)
                {
                    var pattern = _fileOrderPatterns[index];
                    throw new PgUpExitException(
                        $"No files matching the pattern '{pattern}' were found in the '{_batchWorkingDirectory}' working directory. " +
                        $"Please check the pattern and the directory contents, then try again.");

                }
                return matches;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();



        return matchedFiles;
    }

    private string[] DiscoverFiles(FileMatchPredicate[] runOrder, SearchOption searchOption)
    {
        var allFiles = GetSqlFiles(searchOption);
        return allFiles
            .OrderBy(fileFullName =>
            {
                return runOrder
                    .Select(obj =>
                    {
                        var match = runOrder.FirstOrDefault(m => m(fileFullName));
                        if (match is not null)
                        {
                            return Array.IndexOf(runOrder, match);
                        }

                        return int.MaxValue;
                    })
                    .FirstOrDefault(int.MaxValue);
            })
            .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

