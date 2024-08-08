using System;

namespace Solitons.CommandLine;

public static class CliPrompt
{
    /// <summary>
    /// Prompts the user with a yes/no question and returns the answer as a boolean.
    /// Ensures that only 'Y' or 'N' inputs are accepted.
    /// </summary>
    /// <param name="message">The question to display to the user.</param>
    /// <returns>True if the user answers 'Y', false if the user answers 'N'.</returns>
    public static bool GetYesNoAnswer(string message)
    {
        Console.WriteLine($"{message} (Y/N)");

        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true); // Read key without displaying it

            if (keyInfo.Key == ConsoleKey.Y)
            {
                Console.WriteLine("Y");
                return true;
            }
            else if (keyInfo.Key == ConsoleKey.N)
            {
                Console.WriteLine("N");
                return false;
            }
            else
            {
                Console.WriteLine("Invalid input. Please press 'Y' or 'N'.");
            }
        }
    }
}