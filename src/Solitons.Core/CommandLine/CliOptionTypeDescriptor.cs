using System.ComponentModel;

namespace Solitons.CommandLine;

internal abstract record CliOptionTypeDescriptor
{
    public abstract TypeConverter GetDefaultTypeConverter();

    public abstract string CreateRegularExpression(string regexGroupName, string pipeExpression);
}