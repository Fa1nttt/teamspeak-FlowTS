# FlowTS

FlowTS 是一个 Windows 桌面工具，用来把你电脑当前正在使用的软件显示到 TeamSpeak 机器人昵称上。

它的定位是一个轻量级状态桥接工具：FlowTS 会在本机运行一个内置的真实 TeamSpeak 客户端 bot，连接到你的 TeamSpeak 服务器，检测当前前台窗口，并把检测到的软件名称更新到 bot 昵称中。

> English summary: FlowTS is a native Windows desktop app that runs an embedded TeamSpeak client bot and updates the bot nickname based on your current foreground application.

## 功能特性

- 原生 Windows GUI。
- 使用来自 TS3AudioBot 的 `TSLib`，以真实 TeamSpeak 客户端 bot 模式运行。
- 不使用 ServerQuery bot。
- 不启动外部 `TS3AudioBot.exe` 进程。
- 检测当前前台应用，并优先显示更友好的应用名称。
- 支持自定义昵称模板。
- 支持 TeamSpeak 地址、域名和端口。
- 支持服务器密码、默认频道、频道密码。
- 支持托盘后台运行。
- 支持开机后台自启动。
- 支持启动后自动连接。
- 支持 Dev Mode 调试日志开关。
- 固定窗口尺寸，避免缩放导致布局错位。
- Windows x64 自包含单文件发布。

## 图标征集

FlowTS 正在征集一个更适合项目定位的专属 App 图标。

当前早期版本内置的图标是临时图标。如果你愿意为 FlowTS 设计图标，欢迎通过 issue 或 pull request 提交方案。理想的图标应当简洁、小尺寸下容易识别，并能体现 FlowTS 作为 TeamSpeak 状态桥接工具的定位。

## 下载

请在 GitHub Releases 下载最新版本：

```text
FlowTS-v0.1.0-win-x64.zip
```

解压后运行：

```text
FlowTS.exe
```

也可以用后台模式启动：

```text
FlowTS.exe --background
```

## 使用方法

1. 打开 FlowTS。
2. 点击 `设置`。
3. 填写 TeamSpeak 服务器地址和端口。
4. 如果服务器或频道需要密码，填写对应密码。
5. 设置 bot 昵称和昵称模板。
6. 点击 `保存`。
7. 回到主窗口点击 `启动`。

如果启用了托盘后台模式，点击窗口关闭按钮不会退出程序。要完全退出，请右键系统托盘中的 FlowTS 图标，然后选择 `退出`。

## 昵称模板

默认模板：

```text
{bot} | {app}
```

可用变量：

- `{bot}`：设置里的 bot 昵称。
- `{app}`：当前前台应用名称。
- `{title}`：当前前台窗口标题。
- `{short_title}`：缩短后的窗口标题。
- `{process}`：进程名。

示例：

```text
{bot} | {app}
Now: {short_title}
{app} - {process}
```

TeamSpeak 昵称有长度限制，因此 FlowTS 会在必要时裁剪最终昵称。

## 后台运行与开机启动

FlowTS 支持托盘后台运行：

- `关闭或最小化时进入后台`：关闭窗口或最小化时隐藏到系统托盘，而不是退出。
- `开机后台自启动`：写入当前用户的 Windows 启动项。
- `启动后自动连接 TSBot`：程序启动后自动连接 TeamSpeak bot。

开机启动项写入位置：

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

不需要管理员权限。

## Dev Mode

在设置中开启 `Dev Mode` 后，主窗口会显示调试日志。正常使用时建议保持关闭。

## 从源码构建

要求：

- Windows x64。
- .NET SDK 8.0。

如果本机没有全局安装 .NET SDK，可以运行：

```text
Install-DotNet-SDK.cmd
```

然后构建自包含 exe：

```text
Build-FlowTS-Native-Exe.cmd
```

输出位置：

```text
dist\FlowTS\FlowTS.exe
```

构建脚本会发布 `win-x64` 的压缩自包含单文件程序。

## 项目结构

```text
FlowTS.Native/                  FlowTS GUI 和 bot 主程序源码
vendor/TS3AudioBot-source/TSLib 内置 TeamSpeak 客户端库
vendor/libopus/libopus.dll      TSLib 所需的原生 Opus 库
dist/                           本地构建输出，已被 git 忽略
release/                        本地 Release 压缩包，已被 git 忽略
```

## 第三方组件

FlowTS 使用了 TS3AudioBot 项目中的 `TSLib`，并包含 `libopus.dll`。

详情见：

```text
THIRD_PARTY_NOTICES.md
```

## 安全说明

FlowTS 会在 exe 同目录生成配置文件：

```text
flowts-client-config.json
```

如果没有启用 `保存密码到本地`，服务器密码和频道密码不会写入配置文件。

## 许可证

FlowTS 自身的项目许可证暂未最终确定。第三方组件的许可证文件已保留在 `vendor/` 目录，并在 `THIRD_PARTY_NOTICES.md` 中说明。

---

# English

FlowTS is a native Windows desktop app that shows your current foreground application in a TeamSpeak bot nickname.

It runs an embedded TeamSpeak client bot through vendored `TSLib`, connects to your TeamSpeak server, watches the active window on your PC, and updates the bot nickname with the detected application name.

## English Quick Start

1. Download `FlowTS-v0.1.0-win-x64.zip` from GitHub Releases.
2. Extract the zip.
3. Run `FlowTS.exe`.
4. Open `设置` to configure your TeamSpeak server, bot nickname, and nickname template.
5. Click `启动` to connect the bot.

Default template:

```text
{bot} | {app}
```

Available variables: `{bot}`, `{app}`, `{title}`, `{short_title}`, `{process}`.

FlowTS does not use ServerQuery and does not launch an external `TS3AudioBot.exe` process.
