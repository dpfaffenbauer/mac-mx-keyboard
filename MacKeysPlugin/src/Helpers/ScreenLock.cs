namespace Loupedeck.MacKeysPlugin
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    // Locks the screen like Control-Command-Q (Apple menu → Lock Screen).
    internal static class ScreenLock
    {
        private const String LoginFramework = "/System/Library/PrivateFrameworks/login.framework/Versions/Current/login";

        private const String KeystrokeScript = """
            tell application "System Events" to keystroke "q" using {control down, command down}
            """;

        public static void Lock() => Task.Run(() =>
        {
            try
            {
                var result = SACLockScreenImmediate();
                if (result != 0)
                {
                    PluginLog.Warning($"SACLockScreenImmediate returned {result}");
                }

                return;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // a future macOS without the function: press Control-Command-Q instead
                // (needs LogiPluginService under Privacy & Security → Accessibility)
                PluginLog.Warning(ex, "SACLockScreenImmediate is not available - sending Control-Command-Q");
            }

            Shell.RunOsaScript(KeystrokeScript);
        });

        // what Apple menu → Lock Screen calls; a private API, so Lock() falls back to the shortcut
        [DllImport(LoginFramework)]
        private static extern Int32 SACLockScreenImmediate();
    }
}
