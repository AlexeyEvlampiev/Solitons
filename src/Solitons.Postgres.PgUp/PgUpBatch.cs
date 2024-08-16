using System.IO.Compression;
using System.Text.RegularExpressions;
using Solitons.CommandLine;
using Solitons.Data;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpBatch
{
    private readonly string[] _scriptFiles;
    private readonly string _workDir;
    private readonly byte[] _scripts;


    public sealed record Script(string RelativePath, string Content, string Checksum)
    {
        public void Serialize(BinaryWriter writer)
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
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor)
    {
        CustomExecCommand = batch.GetCustomExecCommandText();
        _scriptFiles = batch
            .GetScriptFiles()
            .ToArray();
        _workDir = batch.GetWorkingDirectory();
        using var memory = new MemoryStream();
        using var zipStream = new GZipStream(memory, CompressionLevel.SmallestSize);
        using var writer = new BinaryWriter(zipStream);
        using var alg = new DbCommandTextHasher();
        writer.Write(_scriptFiles.Length);
        foreach (var fileName in _scriptFiles)
        {
            if (Path.IsPathRooted(fileName))
            {
                throw new NotImplementedException();
            }

            var path = Path.Combine(workDir.FullName, _workDir);
            path = Path.Combine(path, fileName);

            if (false == File.Exists(path))
            {
                throw new CliExitException(
                    $"The SQL script specified at '{path}' in the pgup.json file was not found. " +
                    $"Please verify the path and filename are correct.");

            }

            var content = File.ReadAllText(path);
            var checksum = alg.ComputeHash(content);

            content = preProcessor.Transform(content);
            var relativePath = Path
                .GetRelativePath(workDir.FullName, path)
                .Convert(p => Regex.Replace(p, @"\\", "/"));

            var script = new Script(relativePath, content, checksum);
            script.Serialize(writer);
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

}