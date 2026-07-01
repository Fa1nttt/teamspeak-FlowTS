using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using TSLib;
using TSLib.Full;
using TSLib.Helper;
using TSLib.Scheduler;

namespace FlowTS;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var startHidden = args.Any(arg => string.Equals(arg, "--background", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(startHidden));
    }
}

internal sealed class MainForm : Form
{
    private const int MaxDebugLogLines = 500;

    private readonly PictureBox logoBox = new();
    private readonly Label helloLabel = new();
    private readonly Label statusText = new();
    private readonly Label currentAppText = new();
    private readonly Label currentTitleText = new();
    private readonly Button startButton = new();
    private readonly Button stopButton = new();
    private readonly Button settingsButton = new();
    private readonly Button hideButton = new();
    private readonly TextBox debugBox = new();
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly FlowTsBotClient botClient = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly bool startHidden;

    private FlowTsConfig config = FlowTsConfig.Load();
    private string lastNickname = string.Empty;
    private bool allowExit;

    public MainForm(bool startHidden)
    {
        this.startHidden = startHidden;
        Text = "FlowTS";
        Size = new Size(760, 520);
        MinimumSize = Size;
        MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiColors.Background;
        ForeColor = UiColors.Text;
        Font = new Font("Microsoft YaHei UI", 9F);
        Icon = AppIcon.Load();

        BuildUi();
        BuildTrayIcon();
        LoadMainState();

        timer.Tick += async (_, _) => await UpdateNicknameTick();
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized && config.TrayMode) HideToTray(); };
        Shown += async (_, _) => await OnShownStartup();
        FormClosing += async (sender, e) => await OnFormClosing(sender, e);
    }

    private void BuildUi()
    {
        Controls.Clear();
        var header = new Panel { Dock = DockStyle.Top, Height = 102, BackColor = UiColors.Background };
        Controls.Add(header);

        var title = NewLabel("FlowTS", 26, 18, 180, 32, 22F, FontStyle.Bold, UiColors.Text);
        header.Controls.Add(title);
        helloLabel.Text = "你好，" + Environment.UserName;
        helloLabel.Location = new Point(28, 54);
        helloLabel.Size = new Size(360, 24);
        helloLabel.ForeColor = UiColors.Muted;
        header.Controls.Add(helloLabel);

        AddHeaderButton(settingsButton, "设置", 548, 25, 82, OpenSettings);
        AddHeaderButton(hideButton, "后台", 640, 25, 82, HideToTray);
        header.Controls.Add(settingsButton);
        header.Controls.Add(hideButton);

        var margin = 24;
        var gap = 12;
        var top = 118;
        var cardWidth = Math.Max(280, (ClientSize.Width - margin * 2 - gap) / 2);
        var fullWidth = Math.Max(560, ClientSize.Width - margin * 2);

        var statusCard = CreateCard("TSBot 连接状态");
        statusCard.Location = new Point(margin, top);
        statusCard.Size = new Size(cardWidth, 126);
        statusCard.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        statusText.Text = "未连接";
        statusText.Location = new Point(26, 56);
        statusText.Size = new Size(cardWidth - 52, 44);
        statusText.Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);
        statusText.ForeColor = UiColors.Offline;
        statusCard.Controls.Add(statusText);
        Controls.Add(statusCard);

        var actionCard = CreateCard("操作");
        actionCard.Location = new Point(margin + cardWidth + gap, top);
        actionCard.Size = new Size(cardWidth, 126);
        actionCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        AddActionButton(startButton, "启动", 26, 58, 112, async () => await StartBot(false));
        AddActionButton(stopButton, "停止", 150, 58, 112, async () => await StopBot());
        actionCard.Controls.Add(startButton);
        actionCard.Controls.Add(stopButton);
        Controls.Add(actionCard);

        var appCard = CreateCard("当前窗口");
        appCard.Location = new Point(margin, top + 144);
        appCard.Size = new Size(fullWidth, 134);
        appCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        currentAppText.Text = "等待检测";
        currentAppText.Location = new Point(26, 52);
        currentAppText.Size = new Size(fullWidth - 52, 30);
        currentAppText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        currentAppText.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
        currentAppText.ForeColor = UiColors.Text;
        appCard.Controls.Add(currentAppText);
        currentTitleText.Text = "-";
        currentTitleText.Location = new Point(28, 88);
        currentTitleText.Size = new Size(fullWidth - 56, 32);
        currentTitleText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        currentTitleText.ForeColor = UiColors.Muted;
        appCard.Controls.Add(currentTitleText);
        Controls.Add(appCard);

        debugBox.Multiline = true;
        debugBox.ReadOnly = true;
        debugBox.ScrollBars = ScrollBars.Vertical;
        debugBox.BorderStyle = BorderStyle.FixedSingle;
        debugBox.BackColor = Color.FromArgb(12, 16, 21);
        debugBox.ForeColor = Color.FromArgb(175, 187, 202);
        debugBox.Location = new Point(margin, top + 296);
        debugBox.Size = new Size(fullWidth, Math.Max(54, ClientSize.Height - top - 318));
        debugBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        debugBox.Visible = config.DevMode;
        Controls.Add(debugBox);
    }

    private Panel CreateCard(string caption)
    {
        var panel = new RoundedPanel { Padding = new Padding(18), BackColor = UiColors.Card, Radius = 10 };
        panel.Controls.Add(NewLabel(caption, 22, 18, 280, 24, 10F, FontStyle.Regular, UiColors.Muted));
        return panel;
    }

    private static Label NewLabel(string text, int x, int y, int w, int h, float size, FontStyle style, Color color) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(w, h),
        Font = new Font("Microsoft YaHei UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent
    };

    private static void AddHeaderButton(Button button, string text, int x, int y, int w, Action action)
    {
        button.Text = text;
        button.Location = new Point(x, y);
        button.Size = new Size(w, 38);
        StyleButton(button, UiColors.Button, UiColors.Text);
        button.Click += (_, _) => action();
    }

    private static void AddActionButton(Button button, string text, int x, int y, int w, Func<Task> action)
    {
        button.Text = text;
        button.Location = new Point(x, y);
        button.Size = new Size(w, 42);
        StyleButton(button, text == "启动" ? UiColors.Accent : UiColors.Button, Color.White);
        button.Click += async (_, _) => await action();
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }
    private void BuildTrayIcon()
    {
        trayMenu.Items.Clear();
        trayMenu.Items.Add("打开 FlowTS", null, (_, _) => ShowFromTray());
        trayMenu.Items.Add("启动 TSBot", null, async (_, _) => await StartBot(false));
        trayMenu.Items.Add("停止 TSBot", null, async (_, _) => await StopBot());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, async (_, _) => await ExitApplication());
        trayIcon.Icon = Icon ?? SystemIcons.Application;
        trayIcon.Text = "FlowTS";
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = false;
        trayIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private async Task OnShownStartup()
    {
        if (startHidden) HideToTray();
        if (config.AutoStartBot) await StartBot(startHidden);
        MemoryReducer.TrimSoon();
    }

    private async Task OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!allowExit && config.TrayMode && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        timer.Stop();
        trayIcon.Visible = false;
        await botClient.DisconnectAsync();
    }

    private void HideToTray()
    {
        trayIcon.Visible = true;
        ShowInTaskbar = false;
        Hide();
        MemoryReducer.TrimSoon();
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        trayIcon.Visible = config.TrayMode;
    }

    private async Task ExitApplication()
    {
        allowExit = true;
        timer.Stop();
        trayIcon.Visible = false;
        await botClient.DisconnectAsync();
        Close();
    }

    private async Task StartBot(bool silent)
    {
        try
        {
            config = FlowTsConfig.Load();
            ApplyStartupSetting();
            SetStatus("连接中", false);
            startButton.Enabled = false;
            var activity = ActivityReader.GetCurrentActivity();
            var nickname = NicknameFormatter.Format(NicknameFormatter.Render(config.Template, activity, config.BotNickname), config.BotNickname);
            AddLog($"连接到 {config.BuildAddress()} ...");
            var requestedChannel = config.Channel.Trim();
            var defaultChannel = config.BuildDefaultChannel();
            if (!string.IsNullOrWhiteSpace(defaultChannel))
            {
                AddLog(string.Equals(requestedChannel, defaultChannel, StringComparison.Ordinal)
                    ? $"默认频道请求：{defaultChannel}"
                    : $"默认频道请求：{requestedChannel} -> {defaultChannel}");
            }
            await botClient.ConnectAsync(config);
            lastNickname = await botClient.SetNicknameAsync(nickname);
            var channelDiagnostic = await botClient.GetDefaultChannelDiagnosticAsync(config);
            if (!string.IsNullOrWhiteSpace(channelDiagnostic)) AddLog(channelDiagnostic);
            timer.Interval = Math.Max(3, config.IntervalSeconds) * 1000;
            timer.Start();
            UpdateActivityLabels(activity);
            SetStatus("已连接", true);
            AddLog("已连接：" + lastNickname);
        }
        catch (Exception ex)
        {
            timer.Stop();
            await botClient.DisconnectAsync();
            startButton.Enabled = true;
            SetStatus("连接失败", false);
            AddLog("错误：" + ex.Message);
            if (!silent && Visible) MessageBox.Show(ex.Message, "FlowTS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task StopBot()
    {
        timer.Stop();
        await botClient.DisconnectAsync();
        startButton.Enabled = true;
        SetStatus("未连接", false);
        AddLog("已停止");
        MemoryReducer.TrimSoon();
    }

    private async Task UpdateNicknameTick()
    {
        try
        {
            var activity = ActivityReader.GetCurrentActivity();
            UpdateActivityLabels(activity);
            if (!botClient.IsConnected) return;
            var nickname = NicknameFormatter.Format(NicknameFormatter.Render(config.Template, activity, config.BotNickname), config.BotNickname);
            if (!string.Equals(nickname, lastNickname, StringComparison.Ordinal))
            {
                lastNickname = await botClient.SetNicknameAsync(nickname);
                AddLog("昵称更新：" + lastNickname);
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            await botClient.DisconnectAsync();
            startButton.Enabled = true;
            SetStatus("已断开", false);
            AddLog("错误：" + ex.Message);
        }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(config);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        config = dialog.Config;
        config.Save();
        ApplyStartupSetting();
        debugBox.Visible = config.DevMode;
        trayIcon.Visible = config.TrayMode && !Visible;
        AddLog("设置已保存");
        MemoryReducer.TrimSoon();
    }

    private void LoadMainState()
    {
        debugBox.Visible = config.DevMode;
        UpdateActivityLabels(ActivityReader.GetCurrentActivity());
        SetStatus(botClient.IsConnected ? "已连接" : "未连接", botClient.IsConnected);
        AddLog("FlowTS 已启动");
    }

    private void UpdateActivityLabels(ActivityInfo activity)
    {
        currentAppText.Text = TrimForLabel(activity.App, 42);
        currentTitleText.Text = string.IsNullOrWhiteSpace(activity.Title) ? activity.ProcessName : TrimForLabel(activity.Title, 70);
    }

    private static string TrimForLabel(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        value = value.Trim();
        return value.Length <= max ? value : value[..Math.Max(1, max - 3)] + "...";
    }

    private void ApplyStartupSetting() => StartupManager.SetEnabled(config.StartWithWindows);

    private void SetStatus(string text, bool online)
    {
        statusText.Text = text;
        statusText.ForeColor = online ? UiColors.Online : UiColors.Offline;
    }

    private void AddLog(string text)
    {
        if (!config.DevMode && !debugBox.Visible) return;
        debugBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        TrimDebugLogIfNeeded();
        debugBox.SelectionStart = debugBox.Text.Length;
        debugBox.ScrollToCaret();
    }

    private void TrimDebugLogIfNeeded()
    {
        var lines = debugBox.Lines;
        if (lines.Length <= MaxDebugLogLines) return;
        debugBox.Lines = lines[^MaxDebugLogLines..];
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            debugBox.Clear();
            trayIcon.Dispose();
            trayMenu.Dispose();
            botClient.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class SettingsForm : Form
{
    private readonly TextBox serverAddressBox = NewTextBox();
    private readonly NumericUpDown serverPortBox = NewNumber(9987);
    private readonly TextBox serverPasswordBox = NewTextBox(true);
    private readonly TextBox channelBox = NewTextBox();
    private readonly TextBox channelPasswordBox = NewTextBox(true);
    private readonly TextBox botNameBox = NewTextBox();
    private readonly TextBox templateBox = NewTextBox();
    private readonly NumericUpDown intervalBox = NewNumber(8, 3, 60);
    private readonly CheckBox rememberBox = NewCheckBox("保存密码到本地");
    private readonly CheckBox trayModeBox = NewCheckBox("关闭或最小化时进入后台");
    private readonly CheckBox startupBox = NewCheckBox("开机后台自启动");
    private readonly CheckBox autoStartBox = NewCheckBox("启动后自动连接 TSBot");
    private readonly CheckBox devModeBox = NewCheckBox("Dev Mode");
    private readonly Button saveButton = new();
    private readonly Button cancelButton = new();

    public FlowTsConfig Config { get; private set; }

    public SettingsForm(FlowTsConfig config)
    {
        Config = config;
        Text = "FlowTS 设置";
        Size = new Size(620, 650);
        MinimumSize = Size;
        MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UiColors.Background;
        ForeColor = UiColors.Text;
        Font = new Font("Microsoft YaHei UI", 9F);
        Icon = AppIcon.Load();
        BuildUi();
        LoadConfig(config);
    }

    private void BuildUi()
    {
        Controls.Add(new Label { Text = "设置", Location = new Point(28, 22), Size = new Size(180, 34), Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold), ForeColor = UiColors.Text });
        var y = 78;
        AddField("服务器地址", serverAddressBox, 28, y, 350);
        AddField("端口", serverPortBox, 404, y, 150);
        y += 70;
        AddField("服务器密码", serverPasswordBox, 28, y, 250);
        AddField("默认频道", channelBox, 304, y, 250);
        y += 70;
        AddField("频道密码", channelPasswordBox, 28, y, 250);
        AddField("Bot 昵称", botNameBox, 304, y, 250);
        y += 70;
        AddField("昵称模板", templateBox, 28, y, 526);
        y += 70;
        AddField("更新间隔（秒）", intervalBox, 28, y, 160);
        y += 62;
        AddCheck(rememberBox, 28, y, 180);
        AddCheck(trayModeBox, 220, y, 240);
        y += 34;
        AddCheck(startupBox, 28, y, 180);
        AddCheck(autoStartBox, 220, y, 240);
        y += 34;
        AddCheck(devModeBox, 28, y, 180);

        saveButton.Text = "保存";
        saveButton.Location = new Point(348, 545);
        saveButton.Size = new Size(96, 38);
        StyleDialogButton(saveButton, UiColors.Accent, Color.White);
        saveButton.Click += (_, _) => SaveAndClose();
        Controls.Add(saveButton);
        cancelButton.Text = "取消";
        cancelButton.Location = new Point(458, 545);
        cancelButton.Size = new Size(96, 38);
        StyleDialogButton(cancelButton, UiColors.Button, UiColors.Text);
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
        Controls.Add(cancelButton);
    }
    private void AddField(string label, Control control, int x, int y, int w)
    {
        Controls.Add(new Label { Text = label, Location = new Point(x, y), Size = new Size(w, 22), ForeColor = UiColors.Muted });
        control.Location = new Point(x, y + 24);
        control.Size = new Size(w, 28);
        Controls.Add(control);
    }

    private void AddCheck(CheckBox checkBox, int x, int y, int w)
    {
        checkBox.Location = new Point(x, y);
        checkBox.Size = new Size(w, 26);
        Controls.Add(checkBox);
    }

    private void LoadConfig(FlowTsConfig config)
    {
        serverAddressBox.Text = config.ServerAddress;
        serverPortBox.Value = config.ServerPort;
        if (config.RememberPassword)
        {
            serverPasswordBox.Text = config.ServerPassword;
            channelPasswordBox.Text = config.ChannelPassword;
        }
        channelBox.Text = config.Channel;
        botNameBox.Text = config.BotNickname;
        templateBox.Text = config.Template;
        intervalBox.Value = Math.Min(intervalBox.Maximum, Math.Max(intervalBox.Minimum, config.IntervalSeconds));
        rememberBox.Checked = config.RememberPassword;
        trayModeBox.Checked = config.TrayMode;
        startupBox.Checked = config.StartWithWindows;
        autoStartBox.Checked = config.AutoStartBot;
        devModeBox.Checked = config.DevMode;
    }

    private void SaveAndClose()
    {
        Config = Config with
        {
            ServerAddress = string.IsNullOrWhiteSpace(serverAddressBox.Text) ? "127.0.0.1" : serverAddressBox.Text.Trim(),
            ServerPort = (int)serverPortBox.Value,
            ServerPassword = serverPasswordBox.Text,
            Channel = channelBox.Text.Trim(),
            ChannelPassword = channelPasswordBox.Text,
            BotNickname = string.IsNullOrWhiteSpace(botNameBox.Text) ? "FlowTS" : botNameBox.Text.Trim(),
            Template = string.IsNullOrWhiteSpace(templateBox.Text) ? "{bot} | {app}" : templateBox.Text.Trim(),
            IntervalSeconds = (int)intervalBox.Value,
            RememberPassword = rememberBox.Checked,
            TrayMode = trayModeBox.Checked,
            StartWithWindows = startupBox.Checked,
            AutoStartBot = autoStartBox.Checked,
            DevMode = devModeBox.Checked
        };
        DialogResult = DialogResult.OK;
    }

    private static TextBox NewTextBox(bool password = false) => new()
    {
        BackColor = UiColors.Input,
        ForeColor = UiColors.Text,
        BorderStyle = BorderStyle.FixedSingle,
        UseSystemPasswordChar = password
    };

    private static NumericUpDown NewNumber(int value, int min = 1, int max = 65535) => new()
    {
        BackColor = UiColors.Input,
        ForeColor = UiColors.Text,
        Minimum = min,
        Maximum = max,
        Value = value
    };

    private static CheckBox NewCheckBox(string text) => new() { Text = text, ForeColor = UiColors.Text, BackColor = Color.Transparent };

    private static void StyleDialogButton(Button button, Color backColor, Color foreColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }
}

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 10;
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        using var path = RoundedRect(ClientRectangle, Radius);
        Region = new Region(path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var rect = new Rectangle(bounds.Location, new Size(diameter, diameter));
        var path = new GraphicsPath();
        path.AddArc(rect, 180, 90);
        rect.X = bounds.Right - diameter;
        path.AddArc(rect, 270, 90);
        rect.Y = bounds.Bottom - diameter;
        path.AddArc(rect, 0, 90);
        rect.X = bounds.Left;
        path.AddArc(rect, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal static class UiColors
{
    public static readonly Color Background = Color.FromArgb(14, 18, 24);
    public static readonly Color Card = Color.FromArgb(24, 30, 39);
    public static readonly Color Input = Color.FromArgb(18, 23, 31);
    public static readonly Color Button = Color.FromArgb(38, 47, 60);
    public static readonly Color Accent = Color.FromArgb(27, 116, 228);
    public static readonly Color Text = Color.FromArgb(238, 243, 248);
    public static readonly Color Muted = Color.FromArgb(154, 167, 184);
    public static readonly Color Online = Color.FromArgb(44, 203, 139);
    public static readonly Color Offline = Color.FromArgb(235, 91, 91);
}

internal sealed class FlowTsBotClient : IDisposable
{
    private DedicatedTaskScheduler? scheduler;
    private TsFullClient? client;
    public bool IsConnected => client?.Connected == true;

    public async Task ConnectAsync(FlowTsConfig config)
    {
        await DisconnectAsync();
        scheduler = new DedicatedTaskScheduler(new Id(1));
        client = new TsFullClient(scheduler) { QuitMessage = "FlowTS stopped" };
        var identity = LoadOrCreateIdentity(config);
        var connection = new ConnectionDataFull(
            config.BuildAddress(),
            identity,
            username: NicknameFormatter.Format(config.BotNickname, "FlowTS"),
            serverPassword: string.IsNullOrEmpty(config.ServerPassword) ? Password.Empty : Password.FromPlain(config.ServerPassword),
            defaultChannel: config.BuildDefaultChannel(),
            defaultChannelPassword: string.IsNullOrEmpty(config.ChannelPassword) ? Password.Empty : Password.FromPlain(config.ChannelPassword),
            logId: new Id(1));
        var result = await scheduler.InvokeAsync(() => client.Connect(connection));
        if (!result.GetOk(out var error)) throw new InvalidOperationException(error.ToString());
    }

    public async Task<string> SetNicknameAsync(string nickname)
    {
        if (scheduler is null || client is null) throw new InvalidOperationException("TS bot is not connected");
        var clean = NicknameFormatter.Format(nickname, "FlowTS");
        var result = await scheduler.InvokeAsync(() => client.ChangeName(clean));
        if (!result.GetOk(out var error)) throw new InvalidOperationException(error.ToString());
        return clean;
    }

    public async Task<string?> GetDefaultChannelDiagnosticAsync(FlowTsConfig config)
    {
        if (scheduler is null || client is null) return null;
        var requestedChannel = config.Channel.Trim();
        var defaultChannel = config.BuildDefaultChannel();
        if (string.IsNullOrWhiteSpace(defaultChannel)) return null;

        await Task.Delay(500);
        return await scheduler.Invoke(() =>
        {
            if (client is null) return null;
            var self = client.Book.Self();
            if (self is null)
            {
                return "默认频道检查：已连接，但暂时无法读取 bot 当前频道。";
            }

            var currentId = self.Channel.Value;
            var currentChannel = client.Book.CurrentChannel();
            var currentName = currentChannel?.Name.ToString();
            var currentLabel = string.IsNullOrWhiteSpace(currentName) ? $"cid {currentId}" : $"cid {currentId}（{currentName}）";
            var expectedId = FlowTsConfig.TryGetDefaultChannelId(defaultChannel);
            if (expectedId is not null)
            {
                if (currentId == expectedId.Value)
                {
                    return $"默认频道检查：已进入 {currentLabel}。";
                }
                return $"默认频道检查：期望 cid {expectedId.Value}，实际 {currentLabel}。可能原因：cid 不存在、无权限进入、频道密码错误，或服务器将 bot 放回默认频道。";
            }

            return $"默认频道检查：请求频道路径 \"{requestedChannel}\"，当前位于 {currentLabel}。如果没有进入目标频道，请检查频道路径是否与服务器频道树一致。";
        });
    }

    public async Task DisconnectAsync()
    {
        var currentScheduler = scheduler;
        var currentClient = client;
        scheduler = null;
        client = null;
        if (currentScheduler is not null && currentClient is not null)
        {
            try { await currentScheduler.InvokeAsync(() => currentClient.Disconnect()); } catch { }
            currentScheduler.Dispose();
        }
    }

    private static IdentityData LoadOrCreateIdentity(FlowTsConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.IdentityKey))
        {
            var loaded = TsCrypt.LoadIdentityDynamic(config.IdentityKey, config.IdentityOffset);
            if (loaded.GetOk(out var identity)) return identity;
        }
        var created = TsCrypt.GenerateNewIdentity();
        config.IdentityKey = created.PrivateKeyString;
        config.IdentityOffset = created.ValidKeyOffset;
        config.Save();
        return created;
    }

    public void Dispose() => _ = DisconnectAsync();
}

internal sealed record FlowTsConfig
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "flowts-client-config.json");
    public string ServerAddress { get; init; } = "127.0.0.1";
    public int ServerPort { get; init; } = 9987;
    public string ServerPassword { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string ChannelPassword { get; init; } = string.Empty;
    public string BotNickname { get; init; } = "FlowTS";
    public string Template { get; init; } = "{bot} | {app}";
    public int IntervalSeconds { get; init; } = 8;
    public bool RememberPassword { get; init; }
    public bool TrayMode { get; init; } = true;
    public bool StartWithWindows { get; init; }
    public bool AutoStartBot { get; init; }
    public bool DevMode { get; init; }
    public string IdentityKey { get; set; } = string.Empty;
    public ulong IdentityOffset { get; set; }
    public static FlowTsConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var config = JsonSerializer.Deserialize<FlowTsConfig>(File.ReadAllText(ConfigPath)) ?? new FlowTsConfig();
                if (string.IsNullOrWhiteSpace(config.BotNickname)) config = config with { BotNickname = "FlowTS" };
                if (string.IsNullOrWhiteSpace(config.Template)) config = config with { Template = "{bot} | {app}" };
                return config;
            }
        }
        catch { }
        return new FlowTsConfig();
    }

    public void Save()
    {
        var saved = RememberPassword ? this : this with { ServerPassword = string.Empty, ChannelPassword = string.Empty };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true }));
    }

    public string BuildAddress()
    {
        var address = ServerAddress.Trim();
        if (string.IsNullOrWhiteSpace(address)) return "127.0.0.1:" + ServerPort;
        if (address.StartsWith("[", StringComparison.Ordinal)) return address.Contains("]:", StringComparison.Ordinal) ? address : address + ":" + ServerPort;
        if (IPAddress.TryParse(address, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) return "[" + address + "]:" + ServerPort;
        return address.Contains(':') ? address : address + ":" + ServerPort;
    }

    public string BuildDefaultChannel()
    {
        var channel = Channel.Trim();
        if (string.IsNullOrWhiteSpace(channel)) return string.Empty;
        if (channel.StartsWith("/", StringComparison.Ordinal)) return channel;
        return channel.All(static c => c >= '0' && c <= '9') ? "/" + channel : channel;
    }

    public static ulong? TryGetDefaultChannelId(string channel)
    {
        if (!channel.StartsWith("/", StringComparison.Ordinal)) return null;
        return ulong.TryParse(channel[1..], out var channelId) ? channelId : null;
    }
}

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "FlowTS";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (key is null) throw new InvalidOperationException("无法打开 Windows 启动项注册表");
        if (enabled)
        {
            var executable = Application.ExecutablePath;
            key.SetValue(ValueName, $"\"{executable}\" --background", RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}

internal sealed record ActivityInfo(string App, string Title, string ProcessName, int Pid)
{
    public string Describe() => string.IsNullOrWhiteSpace(Title) ? App : App + " - " + Title;
}

internal static class ActivityReader
{
    private static readonly Dictionary<string, string> FriendlyNameOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ts3client"] = "TeamSpeak 3",
        ["ts3client_win32"] = "TeamSpeak 3",
        ["ts3client_win64"] = "TeamSpeak 3",
        ["teamspeak"] = "TeamSpeak",
        ["Code"] = "Visual Studio Code",
        ["devenv"] = "Visual Studio",
        ["chrome"] = "Google Chrome",
        ["msedge"] = "Microsoft Edge",
        ["firefox"] = "Mozilla Firefox",
        ["explorer"] = "文件资源管理器"
    };

    private static readonly HashSet<string> GenericProductNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft Windows Operating System",
        "Windows Operating System"
    };

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public static ActivityInfo GetCurrentActivity()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out var pid);
            var process = Process.GetProcessById((int)pid);
            var processName = process.ProcessName;
            var title = process.MainWindowTitle ?? string.Empty;
            var appName = GetFriendlyAppName(process, title);
            return new ActivityInfo(appName, title, processName, process.Id);
        }
        catch (Exception ex)
        {
            return new ActivityInfo("检测失败", ex.Message, string.Empty, 0);
        }
    }

    private static string GetFriendlyAppName(Process process, string windowTitle)
    {
        var processName = process.ProcessName;
        if (FriendlyNameOverrides.TryGetValue(processName, out var mapped)) return mapped;
        foreach (var candidate in ReadVersionInfoNames(process))
        {
            var normalized = NormalizeAppName(candidate);
            if (!string.IsNullOrWhiteSpace(normalized)) return normalized;
        }
        if (processName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(windowTitle))
        {
            return windowTitle.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? processName;
        }
        return processName;
    }

    private static IEnumerable<string> ReadVersionInfoNames(Process process)
    {
        string? fileName = null;
        try { fileName = process.MainModule?.FileName; } catch { }
        if (string.IsNullOrWhiteSpace(fileName)) yield break;
        FileVersionInfo? versionInfo = null;
        try { versionInfo = FileVersionInfo.GetVersionInfo(fileName); } catch { }
        if (versionInfo is null) yield break;
        if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription)) yield return versionInfo.FileDescription;
        if (!string.IsNullOrWhiteSpace(versionInfo.ProductName)) yield return versionInfo.ProductName;
    }

    private static string NormalizeAppName(string candidate)
    {
        var value = candidate.Trim();
        if (value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) value = value[..^4];
        if (GenericProductNames.Contains(value)) return string.Empty;
        if (value.Equals("TeamSpeak 3 Client", StringComparison.OrdinalIgnoreCase)) return "TeamSpeak 3";
        if (value.Equals("TeamSpeak Client", StringComparison.OrdinalIgnoreCase)) return "TeamSpeak";
        return value;
    }
}

internal static class NicknameFormatter
{
    public static string Render(string template, ActivityInfo activity, string botNickname)
    {
        if (string.IsNullOrWhiteSpace(template)) template = "{bot} | {app}";
        var shortTitle = activity.Title.Length > 21 ? activity.Title[..18] + "..." : activity.Title;
        return template
            .Replace("{bot}", botNickname)
            .Replace("{app}", activity.App)
            .Replace("{title}", activity.Title)
            .Replace("{short_title}", shortTitle)
            .Replace("{process}", activity.ProcessName);
    }

    public static string Format(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) value = fallback;
        var clean = string.Join(" ", value.Replace('|', ' ').Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(clean)) clean = fallback;
        return clean.Length > 30 ? clean[..30] : clean;
    }
}

internal static class MemoryReducer
{
    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, nint minimumWorkingSetSize, nint maximumWorkingSetSize);

    public static async void TrimSoon()
    {
        await Task.Delay(600);
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: false, compacting: false);
            GC.WaitForPendingFinalizers();
            using var process = Process.GetCurrentProcess();
            _ = SetProcessWorkingSetSize(process.Handle, -1, -1);
        }
        catch { }
    }
}

internal static class AppIcon
{
    public static Icon Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "flowts.ico");
        if (File.Exists(path)) return new Icon(path);
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }
}



