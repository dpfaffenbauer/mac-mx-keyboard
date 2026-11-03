# Mac Keys for the Logitech MX Creative Keypad

A Logi Options+ plugin with keys for your Mac: lock the screen with one press, and see and extend how long
[Amphetamine](https://apps.apple.com/app/amphetamine/id937984704) keeps your Mac awake.

> Unofficial community plugin. Not affiliated with or endorsed by Apple, Logitech or the makers of Amphetamine.

## Keys

### Lock screen

**Lock screen** (group *System*) locks the Mac right away, like Control-Command-Q or Apple menu → Lock Screen.

### Amphetamine +15 min / +30 min / +1 h / +2 h / +4 h / +8 h

Each key shows how long the current Amphetamine session still keeps the Mac awake; a press adds the key's time.

| Amphetamine | Key | A press |
|---|---|---|
| timed session | time left (`7h45`, `45m`, `9:59`) in blue; amber below 15 minutes, pulsing amber below 5 | adds the key's time |
| no session | `off` | starts a session of the key's length |
| no time limit / Trigger / until an app quits or a time of day | `∞` / `Trigger` / `on` | nothing |
| not running | `–` | launches Amphetamine |

Amphetamine cannot extend a session, so the plugin restarts it with the time left (rounded up to the minute)
plus the extension, keeping the session's display-sleep and closed-display settings.

The plugin only talks to Amphetamine while it runs and only once an Amphetamine key is shown; macOS then asks
whether *LogiPluginService* may control Amphetamine – allow it (the key shows `no access` otherwise; change it
under *System Settings → Privacy & Security → Automation*).

Texts are English, or German when Logi Options+ runs in German.

## Install

1. Download `MacKeys_x_y.lplug4` from [Releases](../../releases) and double-click it.
2. In Logi Options+, open the MX Creative Keypad, go to *All actions → Installed plugins → Mac Keys* and drag
   the keys you want onto the keypad.

Requirements: macOS, Logi Options+ with an MX Creative Keypad; Amphetamine for the Amphetamine keys.

## How it works

- **Lock screen** calls `SACLockScreenImmediate` from macOS' `login.framework` – what Apple menu → Lock Screen uses.
  It is a private function; should a future macOS drop it, the plugin presses Control-Command-Q instead,
  which needs *LogiPluginService* under *System Settings → Privacy & Security → Accessibility*.
- **Amphetamine** is read through its AppleScript interface (`session time remaining`) every 10 seconds; in
  between, the keys count down on their own. Only while Amphetamine runs – an Apple Event would otherwise
  launch it.

Everything stays on your Mac – no network access, no data collection.

## Troubleshooting

- Plugin log: `~/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/MacKeys.log`

## Development

```bash
# .NET 10 SDK and the Logi Plugin Tool
dotnet tool install --global LogiPluginTool

cd MacKeysPlugin
dotnet build              # Debug: links the build into Logi Plugin Service and reloads it
dotnet build -c Release   # then package:
logiplugintool pack ./bin/Release/ ../dist/MacKeys_1_0.lplug4
logiplugintool verify ../dist/MacKeys_1_0.lplug4
```

Pushing a tag `v*` builds, verifies and attaches the `.lplug4` to a GitHub release.

Code map: `src/Amphetamine/` (Amphetamine session via AppleScript), `src/Helpers/ScreenLock.cs`,
`src/KeyRenderer.cs` (key images), `src/Actions/` (the key actions).

## License

[MIT](LICENSE) – end user terms for the Logitech Marketplace: [EULA](EULA.md)
