using System;

namespace Solitons.CommandLine.ZapCli
{
    public class ClExitRequestedException : Exception
    {
        public int ExitCode { get; }

        public ClExitRequestedException(int exitCode)
        {
            ExitCode = exitCode;
        }
    }
}
