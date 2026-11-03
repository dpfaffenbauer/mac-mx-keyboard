namespace Loupedeck.MacKeysPlugin
{
    using System;

    // Mac keys for the MX Creative Keypad: lock the screen, and see and extend how long
    // Amphetamine keeps the Mac awake.
    public class MacKeysPlugin : Plugin
    {
        public MacKeysPlugin()
        {
            PluginLog.Init(this.Log);
            this.InitStrings();
        }

        public override Boolean UsesApplicationApiOnly => true;

        public override Boolean HasNoApplication => true;

        public override void Load() => this.InitStrings();

        public override void Unload() => AmphetamineMonitor.Instance.Stop();

        private void InitStrings()
        {
            try
            {
                Strings.Init(this.Localization?.LoupedeckLanguage);
            }
            catch (Exception)
            {
                // localization engine not ready yet - keep the system language
            }
        }
    }
}
