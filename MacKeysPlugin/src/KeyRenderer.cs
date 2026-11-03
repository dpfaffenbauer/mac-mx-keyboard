namespace Loupedeck.MacKeysPlugin
{
    using System;

    // Draws the LCD keys. Everything is relative to the key size the service asks for.
    // Dark keys with colour as accent; a key fills with pulsing amber only when it needs you.
    public static class KeyRenderer
    {
        // average character width of the device font relative to its size
        // (measured on the MX Creative Keypad: ~0.48; a little slack for wide letters)
        private const Double CharWidthEm = 0.5;

        private const Int64 AmphetamineSoonSeconds = 15 * 60;
        private const Int64 AmphetamineUrgentSeconds = 5 * 60;

        private static readonly BitmapColor KeyBackground = new(22, 22, 25);
        private static readonly BitmapColor TextColor = new(240, 240, 242);
        private static readonly BitmapColor DimText = new(150, 150, 158);

        private static readonly Theme IdleTheme = new(KeyBackground, new(175, 175, 182), new(115, 115, 124), new(58, 58, 66));
        private static readonly Theme AwakeTheme = new(KeyBackground, TextColor, new(116, 192, 252), new(51, 154, 240));
        private static readonly Theme SoonTheme = new(KeyBackground, TextColor, new(255, 196, 80), new(250, 176, 5));
        private static readonly Theme UrgentThemeA = new(new(250, 176, 5), new(28, 22, 8), new(70, 46, 0), new(250, 176, 5));
        private static readonly Theme UrgentThemeB = new(new(240, 140, 0), new(28, 22, 8), new(70, 40, 0), new(240, 140, 0));
        private static readonly Theme ErrorTheme = new(KeyBackground, TextColor, new(255, 140, 140), new(240, 62, 62));
        private static readonly Theme NotRunningTheme = new(BitmapColor.Black, new(110, 110, 118), new(110, 110, 118), new(40, 40, 46));

        private static Boolean BlinkPhase => DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 2 == 0;

        // Lock screen key: a padlock above the label.
        public static BitmapImage RenderLockScreen(PluginImageSize size)
        {
            using var b = new BitmapBuilder(size);
            var w = b.Width;
            var h = b.Height;
            b.Clear(KeyBackground);
            DrawPadlock(b, w / 2, h * 34 / 100, w * 40 / 100, h * 28 / 100, TextColor, KeyBackground);
            b.DrawText(Strings.LockScreenLabel, 0, h * 69 / 100, w, h * 20 / 100, DimText, Math.Max(9, h * 15 / 100));
            return b.ToImage();
        }

        // Padlock centred on centerX; bodyTop is the top edge of the body (and the shackle's centre).
        // FillCircle takes the centre and the radius.
        public static void DrawPadlock(BitmapBuilder b, Int32 centerX, Int32 bodyTop, Int32 bodyWidth, Int32 bodyHeight, BitmapColor color, BitmapColor background)
        {
            // shackle: a ring whose lower half the body covers
            var radius = bodyWidth * 36 / 100;
            var thickness = Math.Max(3, bodyWidth * 15 / 100);
            b.FillCircle(centerX, bodyTop, radius, color);
            b.FillCircle(centerX, bodyTop, radius - thickness, background);
            b.FillRectangle(centerX - (bodyWidth / 2), bodyTop, bodyWidth, bodyHeight, color);

            // keyhole
            var hole = Math.Max(2, bodyWidth * 12 / 100);
            var holeY = bodyTop + (bodyHeight * 42 / 100);
            b.FillCircle(centerX, holeY, hole, background);
            b.FillRectangle(centerX - (hole / 2), holeY, hole, bodyHeight * 34 / 100, background);
        }

        // Amphetamine key:
        //   ┌──────────────┐
        //   │ Amphetamine  │
        //   │     7h45     │  time left: 7h45 / 45m / 9:59 (counts seconds below 10 minutes)
        //   │   +30 min    │  what a press does
        //   │ ████████████ │
        //   └──────────────┘
        // Blue while the Mac is kept awake, amber below 15 minutes, pulsing amber below 5.
        public static BitmapImage RenderAmphetamine(PluginImageSize size, AmphetamineState state, String extensionLabel)
        {
            using var b = new BitmapBuilder(size);
            var w = b.Width;
            var h = b.Height;
            var theme = AmphetamineTheme(state);
            var (value, hint) = AmphetamineTexts(state, extensionLabel);

            b.Clear(theme.Background);
            if (!IsUrgent(state) && state.Mode != AmphetamineMode.NotRunning)
            {
                var barHeight = Math.Max(4, h * 6 / 100);
                b.FillRectangle(0, h - barHeight, w, barHeight, theme.Accent);
            }

            b.DrawText("Amphetamine", 0, h * 5 / 100, w, h * 17 / 100, IsUrgent(state) ? theme.Status : DimText, Math.Max(9, h * 12 / 100));

            // the ∞ glyph is small for its font size
            var valueSize = value == "∞"
                ? h * 48 / 100
                : Math.Max(10, Math.Min(h * 33 / 100, (Int32)(w * 0.92 / (CharWidthEm * Math.Max(1, value.Length)))));
            b.DrawText(value, 0, h * 25 / 100, w, h * 42 / 100, theme.Title, valueSize);
            b.DrawText(hint, 0, h * 67 / 100, w, h * 20 / 100, theme.Status, Math.Max(9, h * 15 / 100));
            return b.ToImage();
        }

        // Changes whenever the rendered image would change -> drives ActionImageChanged.
        public static String AmphetamineSignature(AmphetamineState state)
        {
            var (value, hint) = AmphetamineTexts(state, "");
            var level = IsUrgent(state) ? (BlinkPhase ? "urgent1" : "urgent0")
                : state.Mode == AmphetamineMode.Timed && state.RemainingSeconds <= AmphetamineSoonSeconds ? "soon"
                : "";
            return $"{state.Mode}|{value}|{hint}|{level}";
        }

        private static Boolean IsUrgent(AmphetamineState state) =>
            state.Mode == AmphetamineMode.Timed && state.RemainingSeconds <= AmphetamineUrgentSeconds;

        private static Theme AmphetamineTheme(AmphetamineState state) => state.Mode switch
        {
            AmphetamineMode.Timed when state.RemainingSeconds <= AmphetamineUrgentSeconds => BlinkPhase ? UrgentThemeA : UrgentThemeB,
            AmphetamineMode.Timed when state.RemainingSeconds <= AmphetamineSoonSeconds => SoonTheme,
            AmphetamineMode.Timed or AmphetamineMode.Infinite or AmphetamineMode.Trigger or AmphetamineMode.Other => AwakeTheme,
            AmphetamineMode.NoAccess => ErrorTheme,
            AmphetamineMode.NotRunning => NotRunningTheme,
            _ => IdleTheme,
        };

        private static (String Value, String Hint) AmphetamineTexts(AmphetamineState state, String extensionLabel) => state.Mode switch
        {
            AmphetamineMode.Timed => (FormatRemaining(state.RemainingSeconds), extensionLabel),
            AmphetamineMode.Off => (Strings.AmphetamineOff, extensionLabel),
            AmphetamineMode.Infinite => ("∞", Strings.AmphetamineNoLimit),
            AmphetamineMode.Trigger => (Strings.AmphetamineTrigger, ""),
            AmphetamineMode.Other => (Strings.AmphetamineOn, ""),
            AmphetamineMode.NoAccess => ("?", Strings.AmphetamineNoAccess),
            _ => ("–", Strings.AmphetamineNotRunning),
        };

        // 7h45, 45m, 9:59
        private static String FormatRemaining(Int64 seconds) =>
            seconds >= 3600 ? $"{seconds / 3600}h{(seconds % 3600) / 60:00}"
            : seconds >= 600 ? $"{seconds / 60}m"
            : $"{seconds / 60}:{seconds % 60:00}";

        private readonly record struct Theme(
            BitmapColor Background,
            BitmapColor Title,
            BitmapColor Status,
            BitmapColor Accent);
    }
}
