# FlowTS v0.1.0

FlowTS 的第一个公开版本。

这个版本已经可以作为基础版使用：程序会在本机运行一个内置 TeamSpeak 客户端 bot，连接到指定服务器后，根据当前前台应用自动更新 bot 昵称。

## 下载

请下载并解压：

```text
FlowTS-v0.1.0-win-x64.zip
```

运行：

```text
FlowTS.exe
```

如需直接后台启动，可以使用：

```text
FlowTS.exe --background
```

## 本版内容

- 原生 Windows 桌面界面。
- 内置 TeamSpeak 客户端 bot，不依赖 ServerQuery。
- 不需要额外运行 TS3AudioBot 程序。
- 支持 TeamSpeak 地址、域名和端口。
- 支持服务器密码、默认频道和频道密码。
- 支持自定义 bot 昵称与昵称模板。
- 自动检测当前前台应用，并更新 bot 昵称。
- 支持托盘后台运行。
- 支持开机后台自启动。
- 支持启动后自动连接。
- 支持 Dev Mode 调试日志。
- 固定窗口尺寸，避免界面缩放后排版错位。
- 提供 Windows x64 自包含单文件版本。

## 使用说明

- 首次使用请先在 `设置` 中填写 TeamSpeak 连接信息。
- 如果服务器或频道需要密码，请在设置中填写对应密码。
- 如果不启用 `保存密码到本地`，密码不会写入配置文件。
- 当前图标为临时图标，项目正在征集更适合 FlowTS 的专属 App 图标。

## 贡献者

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/Fa1nttt">
        <img src="https://github.com/Fa1nttt.png?size=96" width="72" alt="Fa1nttt" />
        <br />
        <sub><b>Fa1nttt</b></sub>
      </a>
      <br />
      项目发起、功能设计、开发与发布
    </td>
  </tr>
</table>

感谢 TS3AudioBot 项目提供的 `TSLib`，FlowTS 的 TeamSpeak 客户端连接能力基于该组件实现。
