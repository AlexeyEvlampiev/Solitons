using System.IO.Compression;

namespace Solitons.Postgres.PgUp.Management.Models;

internal class PgUpAssembly
{
    private readonly byte[] _project;

    private PgUpAssembly(byte[] project)
    {
        _project = project;
    }
    public static async Task<PgUpAssembly> LoadAsync(
        IProject project,
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor)
    {
        await using var memory = new MemoryStream();
        await using var zipStream = new GZipStream(memory, CompressionLevel.SmallestSize);
        await using var writer = new BinaryWriter(zipStream);
        var transactions = project.GetTransactions().ToArray();
        writer.Write(transactions.Length);
        foreach (PgUpTransaction transaction in transactions)
        {
            transaction.Serialize(writer, workDir, preProcessor);
        }
        await zipStream.FlushAsync();
        writer.Flush();
        memory.Position = 0;
        return new PgUpAssembly(memory.ToArray());
    }

}