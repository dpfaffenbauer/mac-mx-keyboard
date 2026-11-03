namespace Loupedeck.MacKeysPlugin
{
    public enum AmphetamineMode
    {
        NotRunning,
        NoAccess,
        Off,
        Timed,
        Infinite,
        Trigger,

        // "while app is running" or "until a time of day" - Amphetamine reports no remaining time
        Other,
    }
}
