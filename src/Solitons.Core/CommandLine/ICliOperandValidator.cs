namespace Solitons.CommandLine;

public interface ICliOperandValidator
{
    CliOperandValidationResult Validate(string value);
}