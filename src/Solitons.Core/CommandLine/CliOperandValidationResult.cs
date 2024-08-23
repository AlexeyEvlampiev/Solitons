namespace Solitons.CommandLine;

public sealed record CliOperandValidationResult(bool Success, string Comment)
{
    public static readonly CliOperandValidationResult Ok = new CliOperandValidationResult(true, "Ok");

    public static CliOperandValidationResult Failure(string comment) => new CliOperandValidationResult(false, comment);
}