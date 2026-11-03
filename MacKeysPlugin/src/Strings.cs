namespace Loupedeck.MacKeysPlugin
{
    using System;
    using System.Globalization;

    // Texts drawn on the keys and shown in Options+. English by default, German when
    // Options+ (or, as a fallback, macOS) runs in German.
    public static class Strings
    {
        private static Boolean _german = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";

        public static void Init(String language)
        {
            if (!String.IsNullOrEmpty(language))
            {
                _german = language.StartsWith("de", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static String AmphetamineGroup => "Amphetamine";

        public static String AmphetamineName(String extension) => $"Amphetamine {extension}";

        public static String AmphetamineOff => _german ? "aus" : "off";

        public static String AmphetamineOn => _german ? "aktiv" : "on";

        public static String AmphetamineNoLimit => _german ? "unbegrenzt" : "no limit";

        public static String AmphetamineTrigger => "Trigger";

        public static String AmphetamineNotRunning => _german ? "nicht offen" : "not running";

        public static String AmphetamineNoAccess => _german ? "kein Zugriff" : "no access";

        public static String SystemGroup => "System";

        public static String LockScreenName => _german ? "Bildschirm sperren" : "Lock screen";

        public static String LockScreenDescription => _german
            ? "Sperrt den Mac sofort (wie Control-Befehl-Q)"
            : "Locks the Mac right away (like Control-Command-Q)";

        public static String LockScreenLabel => _german ? "Sperren" : "Lock";
    }
}
