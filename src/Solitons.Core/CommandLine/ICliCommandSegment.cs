using System;

namespace Solitons.CommandLine;

[Obsolete]
internal interface ICliCommandSegment
{
    string BuildPattern();
}