# FlowTS

FlowTS 是一个 Windows 桌面 GUI 工具，用来把电脑当前正在使用的软件显示到 TeamSpeak bot 昵称上。

当前版本是内置真实 TeamSpeak 客户端模式：FlowTS 直接引用 TS3AudioBot 项目里的 `TSLib`，自己连接 TeamSpeak 服务器并修改自己的客户端昵称。它不使用 ServerQuery，也不启动独立的 `TS3AudioBot.exe` 子进程。

## 当前状态

- 新版程序：`dist\FlowTS\FlowTS.exe`。
- 发布方式：win-x64 self-contained 压缩单文件。
- 运行目录现在只保留一个 exe，项目总体积已从约 1GB 降到约 72MB。
- 支持中文 GUI、托盘后台、开机后台启动、启动后自动连接、Dev Mode 调试信息。

## 启动

推荐直接双击：

```text
dist\FlowTS\FlowTS.exe
```

如果要从项目根目录启动且不出现命令框，双击：

```text
Start-FlowTS.vbs
```

后台启动参数：

```text
dist\FlowTS\FlowTS.exe --background
```

旧的 `FlowTS.cmd` / `Run-FlowTS-Native.cmd` 已移除，因为通过 cmd 启动会出现命令框闪烁。

## 主界面

- 顶部显示 `你好，当前计算机用户名`。
- 主面板显示 TSBot 连接状态。
- 当前窗口面板显示正在使用的应用和窗口标题。
- `设置` 按钮打开独立设置窗口。
- `后台` 按钮隐藏到系统托盘。

## 设置菜单

设置窗口中填写：

- 服务器地址，支持域名。
- 端口，默认通常是 `9987`。
- 服务器密码。
- 默认频道。
- 频道密码。
- Bot 昵称。
- 昵称模板。
- 更新间隔。

运行选项：

- `保存密码到本地`。
- `关闭或最小化时进入后台`。
- `开机后台自启动`。
- `启动后自动连接 TSBot`。
- `Dev Mode`：开启后主界面显示调试日志；关闭后隐藏调试信息。

## 模板变量

- `{bot}`：设置里的 Bot 昵称。
- `{app}`：当前前台程序名称，优先使用应用显示名，例如 TeamSpeak 3、Google Chrome。
- `{title}`：当前窗口标题。
- `{short_title}`：裁剪后的窗口标题。
- `{process}`：进程名。

默认模板：

```text
{bot} | {app}
```

## 后台与自启动

- 关闭窗口或最小化时进入托盘，不退出程序。
- 开机自启动写入当前用户注册表：

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

- 自启动命令会带 `--background`，因此开机后不会弹出主窗口。
- 真正退出程序：右键系统托盘里的 FlowTS 图标，选择 `退出`。

## 内存与体积

- FlowTS 不启动外部 `TS3AudioBot.exe`，没有 WebView。
- 后台启动不会显示主窗口；设置窗口按需创建。
- 隐藏到托盘或停止 bot 后会主动释放一轮工作集。
- 当前后台空闲测试：工作集约 7.6MB，私有内存约 44.9MB。
- 最终发布 exe 约 70.7MB。

## 重新构建

如果需要重新构建，先运行：

```text
Install-DotNet-SDK.cmd
```

然后运行：

```text
Build-FlowTS-Native-Exe.cmd
```

构建完成后可以删除 `.dotnet`、`FlowTS.Native\bin`、`FlowTS.Native\obj` 来再次减小项目目录体积。

## 保留文件说明

- `FlowTS.Native`：FlowTS GUI 和 bot 主程序源码。
- `vendor/TS3AudioBot-source/TSLib`：真实 TeamSpeak 客户端库源码。
- `vendor/libopus/libopus.dll`：TSLib 语音客户端所需原生库。
- `dist/FlowTS/FlowTS.exe`：最终可运行程序。
