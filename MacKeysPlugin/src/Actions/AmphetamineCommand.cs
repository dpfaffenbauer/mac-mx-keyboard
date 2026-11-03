namespace Loupedeck.MacKeysPlugin
{
    using System;
    using System.Collections.Generic;

    // "Amphetamine +15 min / +30 min / +1 h / +2 h / +4 h / +8 h": shows how long Amphetamine keeps the Mac
    // awake; a press adds the key's time to the session (or starts one when none runs).
    public class AmphetamineCommand : PluginDynamicCommand
    {
        // parameter -> (minutes, label)
        private static readonly (String Parameter, Int32 Minutes, String Label)[] Extensions =
        {
            ("add15", 15, "+15 min"),
            ("add30", 30, "+30 min"),
            ("add60", 60, "+1 h"),
            ("add120", 120, "+2 h"),
            ("add240", 240, "+4 h"),
            ("add480", 480, "+8 h"),
        };

        // No caption on keys, the image carries everything. An empty string makes the service fall
        // back to the action name; a zero-width space is neither empty nor whitespace and draws nothing.
        private const String InvisibleCaption = "\u200B";

        private readonly Dictionary<String, String> _signatures = new();
        private readonly Object _gate = new();

        public AmphetamineCommand()
            : base()
        {
            foreach (var extension in Extensions)
            {
                this.AddParameter(extension.Parameter, Strings.AmphetamineName(extension.Label), Strings.AmphetamineGroup);
            }

            AmphetamineMonitor.Instance.Updated += this.OnUpdated;
        }

        protected override void RunCommand(String actionParameter)
        {
            var extension = Find(actionParameter);
            if (extension.Minutes > 0)
            {
                AmphetamineMonitor.Instance.Extend(extension.Minutes);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            // the key is on screen: from now on ask Amphetamine for its state
            AmphetamineMonitor.Instance.EnsureStarted();
            return KeyRenderer.RenderAmphetamine(imageSize, AmphetamineMonitor.Instance.Current, Find(actionParameter).Label);
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            imageSize == PluginImageSize.None ? Strings.AmphetamineName(Find(actionParameter).Label) : InvisibleCaption;

        private void OnUpdated()
        {
            // raised from the monitor's timer and after a press, possibly at the same time
            var signature = KeyRenderer.AmphetamineSignature(AmphetamineMonitor.Instance.Current);
            lock (this._gate)
            {
                foreach (var extension in Extensions)
                {
                    if (!this._signatures.TryGetValue(extension.Parameter, out var previous) || previous != signature)
                    {
                        this._signatures[extension.Parameter] = signature;
                        this.ActionImageChanged(extension.Parameter);
                    }
                }
            }
        }

        private static (String Parameter, Int32 Minutes, String Label) Find(String actionParameter) =>
            Array.Find(Extensions, e => e.Parameter == actionParameter);
    }
}
