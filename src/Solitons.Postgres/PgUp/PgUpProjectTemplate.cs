using Solitons.CommandLine;

public sealed class PgUpProjectTemplate(string capture) : CliParameter<string>(capture, "--template|-t", "")
{
    public PgUpProjectTemplate() : this("basic")
    {
        
    }
}
