namespace Loupedeck.MacKeysPlugin
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    // Reads the session state of Amphetamine (https://apps.apple.com/app/amphetamine/id937984704)
    // through its AppleScript interface and extends sessions.
    //
    // Amphetamine is only asked while it runs: an Apple Event to an app that is not running would
    // launch it, and one to an app that is not installed would make osascript ask where it is.
    // Between two queries the remaining time is counted down locally.
    public sealed class AmphetamineMonitor
    {
        public const String BundleId = "com.if.Amphetamine";

        private const String ProcessName = "Amphetamine";
        private const Int32 TickMs = 1000;
        private const Int64 QueryEveryMs = 10_000;

        // the first query can sit behind the macOS "allow to control Amphetamine?" prompt
        private const Int32 QueryTimeoutMs = 60_000;

        // returns Amphetamine's code: seconds left, 0 = no limit, -1 = Trigger,
        // -2 = app/time based, -3 = no session
        private const String QueryScript = """
            tell application id "com.if.Amphetamine" to return (session time remaining) as text
            """;

        // argv[1] = minutes to add. Amphetamine cannot extend a session, so the session is
        // restarted with the time left (rounded up to the minute) plus the extension, keeping
        // its display sleep and closed-display settings. Without a session, a new one is started.
        // Infinite, Trigger and app/time based sessions are left alone.
        private const String ExtendScript = """
            on run argv
                set addMinutes to (item 1 of argv) as integer
                tell application id "com.if.Amphetamine"
                    set remaining to session time remaining
                    if remaining > 0 then
                        set total to ((remaining + 59) div 60) + addMinutes
                    else if remaining is -3 then
                        set total to addMinutes
                    else
                        return remaining as text
                    end if
                    set displaySleep to display sleep allowed
                    set closedDisplay to closed display mode enabled
                    start new session with options {duration:total, interval:minutes, displaySleepAllowed:displaySleep}
                    if (closed display mode enabled) is not closedDisplay then
                        if closedDisplay then
                            enable closed display mode
                        else
                            disable closed display mode
                        end if
                    end if
                    return (session time remaining) as text
                end tell
            end run
            """;

        private readonly Object _gate = new();
        private readonly Object _scriptGate = new();
        private Timer _timer;
        private Int32 _querying;

        private AmphetamineMode _mode = AmphetamineMode.NotRunning;
        private Int64 _remainingAtQuery;
        private Int64 _queriedAt = Int64.MinValue / 2;

        public static AmphetamineMonitor Instance { get; } = new AmphetamineMonitor();

        // Raised once per second while the monitor runs.
        public event Action Updated;

        public AmphetamineState Current
        {
            get
            {
                lock (this._gate)
                {
                    if (this._mode != AmphetamineMode.Timed)
                    {
                        return new AmphetamineState(this._mode, 0);
                    }

                    var elapsed = (Environment.TickCount64 - this._queriedAt) / 1000;
                    return new AmphetamineState(AmphetamineMode.Timed, Math.Max(0, this._remainingAtQuery - elapsed));
                }
            }
        }

        // Started by the first key that shows Amphetamine, so that users without such a key
        // never get the macOS automation prompt for Amphetamine.
        public void EnsureStarted()
        {
            lock (this._gate)
            {
                if (this._timer != null)
                {
                    return;
                }

                this._timer = new Timer(_ => this.Tick(), null, 0, TickMs);
            }

            PluginLog.Info("Amphetamine monitor started");
        }

        public void Stop()
        {
            lock (this._gate)
            {
                this._timer?.Dispose();
                this._timer = null;
            }
        }

        // Adds minutes to a timed session or starts a session if none is running.
        public void Extend(Int32 minutes) => Task.Run(() =>
        {
            try
            {
                switch (this.Current.Mode)
                {
                    case AmphetamineMode.NotRunning:
                        PluginLog.Info("Amphetamine is not running - launching it");
                        Shell.Run("/usr/bin/open", "-b", BundleId);
                        this.ForceQuery();
                        return;
                    case AmphetamineMode.Timed:
                    case AmphetamineMode.Off:
                    case AmphetamineMode.NoAccess:
                        break;
                    default:
                        // nothing to extend: no time limit, or a Trigger/app/time based session
                        PluginLog.Info($"Amphetamine session is {this.Current.Mode} - not extended");
                        return;
                }

                if (!IsRunning())
                {
                    this.ForceQuery();
                    return;
                }

                Shell.Result result;
                lock (this._scriptGate)
                {
                    result = Shell.ExecuteOsaScript(ExtendScript, QueryTimeoutMs, minutes.ToString());
                }

                if (result.ExitCode != 0)
                {
                    PluginLog.Warning($"Extending the Amphetamine session failed ({result.ExitCode}): {result.Error}");
                }

                this.Apply(result);
                PluginLog.Info($"Amphetamine +{minutes} min -> {this.Current.Mode} {this.Current.RemainingSeconds}s");
                this.Updated?.Invoke();
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Could not extend the Amphetamine session");
            }
        });

        private void Tick()
        {
            try
            {
                var current = this.Current;
                var due = Environment.TickCount64 - Volatile.Read(ref this._queriedAt) >= QueryEveryMs
                    || (current.Mode == AmphetamineMode.Timed && current.RemainingSeconds == 0);
                if (due && Interlocked.Exchange(ref this._querying, 1) == 0)
                {
                    try
                    {
                        this.Query();
                    }
                    finally
                    {
                        Volatile.Write(ref this._querying, 0);
                    }
                }

                this.Updated?.Invoke();
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Amphetamine refresh failed");
            }
        }

        private void ForceQuery()
        {
            lock (this._gate)
            {
                this._queriedAt = Int64.MinValue / 2;
            }
        }

        private void Query()
        {
            if (!IsRunning())
            {
                this.SetState(AmphetamineMode.NotRunning, 0);
                return;
            }

            Shell.Result result;
            lock (this._scriptGate)
            {
                result = Shell.ExecuteOsaScript(QueryScript, QueryTimeoutMs);
            }

            this.Apply(result);
        }

        private void Apply(Shell.Result result)
        {
            if (result.ExitCode != 0 || !Int64.TryParse(result.Output, out var code))
            {
                // most likely "not allowed to send Apple events to Amphetamine"
                if (this.SetState(AmphetamineMode.NoAccess, 0))
                {
                    PluginLog.Warning($"Cannot read the Amphetamine session ({result.ExitCode}): {result.Error}");
                }

                return;
            }

            var mode = code switch
            {
                > 0 => AmphetamineMode.Timed,
                0 => AmphetamineMode.Infinite,
                -1 => AmphetamineMode.Trigger,
                -2 => AmphetamineMode.Other,
                _ => AmphetamineMode.Off,
            };
            this.SetState(mode, Math.Max(0, code));
        }

        // Returns true if the mode changed.
        private Boolean SetState(AmphetamineMode mode, Int64 remaining)
        {
            Boolean changed;
            lock (this._gate)
            {
                changed = mode != this._mode;
                this._mode = mode;
                this._remainingAtQuery = remaining;
                this._queriedAt = Environment.TickCount64;
            }

            if (changed)
            {
                PluginLog.Info($"Amphetamine: {mode}");
            }

            return changed;
        }

        private static Boolean IsRunning()
        {
            var processes = Process.GetProcessesByName(ProcessName);
            foreach (var process in processes)
            {
                process.Dispose();
            }

            return processes.Length > 0;
        }
    }
}
