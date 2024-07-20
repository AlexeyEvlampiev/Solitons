using System;

namespace Solitons.CommandLine;

[Flags]
public enum CliAsciiHeaderCondition
{
    Always = -1,
    OnNoArguments = 1
}