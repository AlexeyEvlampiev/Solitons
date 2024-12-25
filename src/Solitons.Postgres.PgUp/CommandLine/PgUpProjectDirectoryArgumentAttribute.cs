using System.ComponentModel;
using Solitons.CommandLine.Reflection;

namespace Solitons.Postgres.PgUp.CommandLine;

sealed class PgUpProjectDirectoryArgumentAttribute : CliArgumentAttribute
{
    public PgUpProjectDirectoryArgumentAttribute(string parameterName)
        : base(parameterName, "PgUp project directory.")
    {
        Name = "PROJECTDIR";
    }

    public override bool CanAccept(Type argumentType, out TypeConverter converter)
    {
        if (argumentType == typeof(string))
        {
            converter = new StringConverter();
            return true;
        }

        converter = TypeDescriptor.GetConverter(argumentType);
        return false;
    }
}