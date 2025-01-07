using System;
using System.IO;
using System.Text;

namespace Solitons.IO;

public class FolderStructurePrinter
{    /// <summary>
    /// Generates an ASCII folder structure for the specified directory.
    /// </summary>
    /// <param name="projectDir">The root directory to generate the structure for.</param>
    /// <returns>A string representing the folder structure.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="projectDir"/> is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
    public static string GenerateFolderStructure(DirectoryInfo projectDir)
    {
        if (projectDir == null)
            throw new ArgumentNullException(nameof(projectDir));
        if (!projectDir.Exists)
            throw new DirectoryNotFoundException($"The directory '{projectDir.FullName}' does not exist.");

        var output = new StringBuilder();
        GenerateFolderStructureRecursive(projectDir, output, string.Empty, true);
        return output.ToString();
    }

    private static void GenerateFolderStructureRecursive(DirectoryInfo dir, StringBuilder output, string prefix, bool isLast)
    {
        // Print the current directory
        output.AppendLine($"{prefix}{(isLast ? "└──" : "├──")}{dir.Name}");

        // Update prefix for child entries
        var newPrefix = prefix + (isLast ? "    " : "│   ");

        // Get all subdirectories and files
        var subDirs = dir.GetDirectories();
        var files = dir.GetFiles();

        // Iterate through subdirectories
        for (int i = 0; i < subDirs.Length; i++)
        {
            GenerateFolderStructureRecursive(subDirs[i], output, newPrefix, i == subDirs.Length - 1 && files.Length == 0);
        }

        // Iterate through files
        for (int i = 0; i < files.Length; i++)
        {
            output.AppendLine($"{newPrefix}{(i == files.Length - 1 ? "└──" : "├──")}{files[i].Name}");
        }
    }
}