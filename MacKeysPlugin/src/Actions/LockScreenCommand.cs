namespace Loupedeck.MacKeysPlugin
{
    using System;

    // "Lock screen": locks the Mac (like Control-Command-Q).
    public class LockScreenCommand : PluginDynamicCommand
    {
        public LockScreenCommand()
            : base(displayName: Strings.LockScreenName, description: Strings.LockScreenDescription, groupName: Strings.SystemGroup)
        {
        }

        protected override void RunCommand(String actionParameter) => ScreenLock.Lock();

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) =>
            KeyRenderer.RenderLockScreen(imageSize);

        // the key image carries the label (zero-width space: see AmphetamineCommand)
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            imageSize == PluginImageSize.None ? Strings.LockScreenName : "\u200B";
    }
}
