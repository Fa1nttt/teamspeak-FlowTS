# FlowTS

FlowTS is a small Windows desktop tool that shows your current foreground application in a TeamSpeak bot nickname.

It works like a lightweight status bridge: FlowTS runs a real embedded TeamSpeak client bot, connects to your TeamSpeak server, watches the active window on your PC, and updates the bot nickname with the detected application name.

## Features

- Native Windows GUI.
- Real TeamSpeak client bot mode through vendored `TSLib` from TS3AudioBot.
- No ServerQuery bot.
- No external `TS3AudioBot.exe` process.
- Foreground application detection with friendly application names.
- Custom nickname template.
- TeamSpeak address/domain and port support.
- Optional server password, default channel, and channel password.
- Tray background mode.
- Start with Windows in background mode.
- Auto connect on launch.
- Dev Mode debug log toggle.
- Fixed-size windows for stable layout.
- Self-contained Windows x64 single-file release build.

## Download

Download the latest release package from GitHub Releases:

```text
FlowTS-v0.1.0-win-x64.zip
```

Extract the zip and run:

```text
FlowTS.exe
```

You can also start it hidden in the background:

```text
FlowTS.exe --background
```

## Usage

1. Open FlowTS.
2. Click `设置`.
3. Fill in your TeamSpeak server address and port.
4. Fill in optional server/channel passwords if needed.
5. Set the bot nickname and nickname template.
6. Click `保存`.
7. Click `启动` on the main window.

To fully exit FlowTS while tray mode is enabled, right-click the tray icon and choose `退出`.

## Nickname Template

The default template is:

```text
{bot} | {app}
```

Available variables:

- `{bot}`: bot nickname from settings.
- `{app}`: friendly name of the current foreground application.
- `{title}`: current foreground window title.
- `{short_title}`: shortened window title.
- `{process}`: process name.

Examples:

```text
{bot} | {app}
Now: {short_title}
{app} - {process}
```

TeamSpeak nicknames are limited in length, so FlowTS trims the final nickname when needed.

## Background and Startup

FlowTS supports tray/background usage:

- `关闭或最小化时进入后台`: hides the window to the system tray instead of exiting.
- `开机后台自启动`: writes a current-user Windows startup entry.
- `启动后自动连接 TSBot`: connects automatically after launch.

The startup entry is stored under:

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

No administrator permission is required.

## Dev Mode

Enable `Dev Mode` in settings to show debug logs in the main window. Keep it disabled for normal usage.

## Build from Source

Requirements:

- Windows x64.
- .NET SDK 8.0.

If .NET SDK is not installed globally, run:

```text
Install-DotNet-SDK.cmd
```

Then build the self-contained executable:

```text
Build-FlowTS-Native-Exe.cmd
```

Output:

```text
dist\FlowTS\FlowTS.exe
```

The build script publishes a compressed self-contained single-file executable for `win-x64`.

## Repository Layout

```text
FlowTS.Native/                  FlowTS GUI and bot application source
vendor/TS3AudioBot-source/TSLib Vendored TeamSpeak client library
vendor/libopus/libopus.dll      Native Opus library required by TSLib
dist/                           Local build output, ignored by git
release/                        Local release zip output, ignored by git
```

## Third-party Components

FlowTS vendors parts of TS3AudioBot's `TSLib` and includes `libopus.dll`.

See:

```text
THIRD_PARTY_NOTICES.md
```

## Security Notes

FlowTS stores configuration next to the executable in `flowts-client-config.json`.

If `保存密码到本地` is disabled, server and channel passwords are not written to that config file.

## License

FlowTS project licensing has not been finalized yet. Third-party licenses are preserved under `vendor/` and documented in `THIRD_PARTY_NOTICES.md`.
