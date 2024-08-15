using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Solitons.Postgres.PgUp.Models;

public sealed class PgUpStage
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

    public PgUpStage(
        IPgUpStage stage, 
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor)
    {
        if (stage.HasCustomExecutor(out var cex))
        {
            CustomExecutorInfo = new PgUpCustomExecutorInfo(cex);
        }
        _scriptFiles = stage
            .GetScriptFiles()
            .ToArray();
        _workDir = stage.GetWorkingDirectory();
        using var memory = new MemoryStream();
        using var zipStream = new GZipStream(memory, CompressionLevel.SmallestSize);
        using var writer = new BinaryWriter(zipStream);
        using var crypto = SHA256.Create();
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
                throw new NotImplementedException();
            }

            var content = File.ReadAllText(path);
            var checksum = crypto
                .ComputeHash(Encoding.UTF8.GetBytes(content))
                .Select(b => b.ToString("x2"))
                .Join(string.Empty);

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

    public PgUpCustomExecutorInfo? CustomExecutorInfo { get; }


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