namespace Loupedeck.MacKeysPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    // Runs helper processes (osascript, open, ps) and returns their output.
    internal static class Shell
    {
        private const Int32 DefaultTimeoutMs = 5000;

        // Trimmed stdout, or null if the process could not run or timed out.
        // A non-zero exit code is logged.
        public static String Run(String fileName, params String[] arguments)
        {
            var result = Execute(fileName, arguments, DefaultTimeoutMs);
            if (result.ExitCode > 0)
            {
                PluginLog.Warning($"{fileName} exited {result.ExitCode}: {result.Error}");
            }

            return result.Output;
        }

        public static String RunOsaScript(String script, params String[] arguments) =>
            Run("/usr/bin/osascript", OsaArguments(script, arguments));

        // Like RunOsaScript, but leaves logging to the caller.
        public static Result ExecuteOsaScript(String script, Int32 timeoutMs, params String[] arguments) =>
            Execute("/usr/bin/osascript", OsaArguments(script, arguments), timeoutMs);

        // ExitCode -1: the process could not be started or timed out.
        public static Result Execute(String fileName, String[] arguments, Int32 timeoutMs)
        {
            var info = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var argument in arguments)
            {
                info.ArgumentList.Add(argument);
            }

            using var process = Process.Start(info);
            if (process == null)
            {
                return new Result(-1, null, "could not start");
            }

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill();
                PluginLog.Warning($"{fileName} timed out");
                return new Result(-1, null, "timed out");
            }

            return new Result(process.ExitCode, output.Result.Trim(), error.Result.Trim());
        }

        // osascript takes the script line by line via -e, followed by the run handler's argv
        private static String[] OsaArguments(String script, String[] arguments)
        {
            var args = new List<String>();
            foreach (var line in script.Split('\n'))
            {
                args.Add("-e");
                args.Add(line);
            }

            args.AddRange(arguments);
            return args.ToArray();
        }

        public readonly record struct Result(Int32 ExitCode, String Output, String Error);
    }
}
