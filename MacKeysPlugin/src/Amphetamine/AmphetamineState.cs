namespace Loupedeck.MacKeysPlugin
{
    using System;

    // What the keys show: mode plus, for a timed session, the seconds left right now.
    public readonly record struct AmphetamineState(AmphetamineMode Mode, Int64 RemainingSeconds);
}
