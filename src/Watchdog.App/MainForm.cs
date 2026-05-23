using Watchdog.Core;

namespace Watchdog.App;

public sealed class MainForm : Form
{
    private readonly TextBox _exePathTextBox = new();
    private readonly NumericUpDown _intervalSecondsInput = new();
    private readonly NumericUpDown _unresponsiveSecondsInput = new();
    private readonly Button _browseButton = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _checkNowButton = new();
    private readonly Label _statusValueLabel = new();
    private readonly Label _statusHintLabel = new();
    private readonly ListView _logListView = new();
    private readonly PictureBox _dogPictureBox = new();
    private readonly Panel _brandPanel = new();
    private readonly Panel _settingsPanel = new();
    private readonly Panel _statusPanel = new();
    private readonly Panel _logPanel = new();

    private ProcessWatchdog? _watchdog;

    public MainForm()
    {
        InitializeWindow();
        BuildLayout();
        SetMonitoringState(false);
        AddLog("系统", "请选择要守护的 EXE，然后点击“开始监控”。");
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (_watchdog is not null)
        {
            await _watchdog.DisposeAsync();
        }

        base.OnFormClosing(e);
    }

    private void InitializeWindow()
    {
        Text = "看门狗工具";
        Icon = IconFactory.CreateDogIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);
        Size = new Size(1080, 720);
        BackColor = Color.FromArgb(240, 244, 249);
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 26),
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 306));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildTopPanel(), 0, 0);
        root.Controls.Add(BuildLogPanel(), 0, 1);
    }

    private Control BuildTopPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 0, 0, 16)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 252));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(BuildBrandPanel(), 0, 0);
        panel.Controls.Add(BuildWorkPanel(), 1, 0);

        return panel;
    }

    private Control BuildBrandPanel()
    {
        var panel = _brandPanel;
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.White;
        panel.Padding = new Padding(14);
        panel.Paint += DrawRoundedPanel;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(layout);

        _dogPictureBox.Dock = DockStyle.Fill;
        _dogPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        _dogPictureBox.Image = IconFactory.CreateDogImage(224);
        layout.Controls.Add(_dogPictureBox, 0, 0);

        return panel;
    }

    private Control BuildWorkPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 166));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(BuildHeader(), 0, 0);
        panel.Controls.Add(BuildSettingsPanel(), 0, 1);
        panel.Controls.Add(BuildActionPanel(), 0, 2);

        return panel;
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 204));

        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        panel.Controls.Add(titlePanel, 0, 0);

        var titleLabel = new Label
        {
            Text = "进程监控配置",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 31, 42),
            TextAlign = ContentAlignment.BottomLeft
        };
        titlePanel.Controls.Add(titleLabel, 0, 0);

        var subtitleLabel = new Label
        {
            Text = "选择目标 EXE，工具会按间隔检查并自动恢复异常进程。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(91, 103, 122),
            TextAlign = ContentAlignment.TopLeft
        };
        titlePanel.Controls.Add(subtitleLabel, 0, 1);

        var statusPanel = _statusPanel;
        statusPanel.Dock = DockStyle.Fill;
        statusPanel.BackColor = Color.White;
        statusPanel.Padding = new Padding(16, 8, 16, 8);
        statusPanel.Paint += DrawRoundedPanel;
        panel.Controls.Add(statusPanel, 1, 0);

        var statusLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        statusPanel.Controls.Add(statusLayout);

        _statusValueLabel.Dock = DockStyle.Fill;
        _statusValueLabel.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
        _statusValueLabel.TextAlign = ContentAlignment.BottomLeft;
        statusLayout.Controls.Add(_statusValueLabel, 0, 0);

        _statusHintLabel.Dock = DockStyle.Fill;
        _statusHintLabel.ForeColor = Color.FromArgb(91, 103, 122);
        _statusHintLabel.TextAlign = ContentAlignment.TopLeft;
        statusLayout.Controls.Add(_statusHintLabel, 0, 1);

        return panel;
    }

    private Control BuildSettingsPanel()
    {
        var panel = _settingsPanel;
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.White;
        panel.Padding = new Padding(20, 16, 20, 16);
        panel.Paint += DrawRoundedPanel;

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        panel.Controls.Add(grid);

        var pathRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        grid.Controls.Add(pathRow, 0, 0);

        pathRow.Controls.Add(CreateFieldLabel("EXE 路径"), 0, 0);

        _exePathTextBox.Dock = DockStyle.Fill;
        _exePathTextBox.PlaceholderText = @"例如：C:\Apps\Server.exe";
        _exePathTextBox.Margin = new Padding(0, 6, 12, 6);
        pathRow.Controls.Add(_exePathTextBox, 1, 0);

        _browseButton.Text = "选择 EXE";
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.Margin = new Padding(0, 6, 0, 6);
        ConfigureBrowseButton(_browseButton);
        _browseButton.Click += BrowseButton_Click;
        pathRow.Controls.Add(_browseButton, 2, 0);

        grid.Controls.Add(BuildLabeledNumberField("检查间隔", _intervalSecondsInput, 10, 1, 3600, "秒"), 0, 1);
        grid.Controls.Add(BuildLabeledNumberField("无响应判定", _unresponsiveSecondsInput, 5, 1, 300, "秒后重启"), 0, 2);

        return panel;
    }

    private Control BuildActionPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Padding = new Padding(0, 16, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));

        ConfigurePrimaryButton(_startButton, "开始监控");
        _startButton.Click += StartButton_Click;
        panel.Controls.Add(_startButton, 0, 0);

        ConfigureSecondaryButton(_stopButton, "停止监控");
        _stopButton.Click += StopButton_Click;
        panel.Controls.Add(_stopButton, 1, 0);

        ConfigureSecondaryButton(_checkNowButton, "立即检查");
        _checkNowButton.Click += CheckNowButton_Click;
        panel.Controls.Add(_checkNowButton, 2, 0);

        var noteLabel = new Label
        {
            Text = "启动后锁定配置，停止后可修改。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(91, 103, 122),
            TextAlign = ContentAlignment.MiddleRight
        };
        panel.Controls.Add(noteLabel, 4, 0);

        return panel;
    }

    private Control BuildLogPanel()
    {
        var panel = _logPanel;
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.White;
        panel.Padding = new Padding(18);
        panel.Paint += DrawRoundedPanel;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(layout);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.Controls.Add(header, 0, 0);

        var titleLabel = new Label
        {
            Text = "运行日志",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 31, 42),
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(titleLabel, 0, 0);

        var keepLabel = new Label
        {
            Text = "最多保留 500 条",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(91, 103, 122),
            TextAlign = ContentAlignment.MiddleRight
        };
        header.Controls.Add(keepLabel, 1, 0);

        _logListView.Dock = DockStyle.Fill;
        _logListView.View = View.Details;
        _logListView.FullRowSelect = true;
        _logListView.GridLines = false;
        _logListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _logListView.BackColor = Color.FromArgb(18, 24, 33);
        _logListView.ForeColor = Color.FromArgb(226, 233, 243);
        _logListView.Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        _logListView.Columns.Add("时间", 162);
        _logListView.Columns.Add("类型", 92);
        _logListView.Columns.Add("内容", 720);
        _logListView.Resize += (_, _) => ResizeLogColumns();
        layout.Controls.Add(_logListView, 0, 1);

        return panel;
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        if (!TryCreateOptions(out var options))
        {
            return;
        }

        if (_watchdog is not null)
        {
            await _watchdog.DisposeAsync();
        }

        _watchdog = new ProcessWatchdog(new SystemProcessController(), options);
        _watchdog.LogReceived += Watchdog_LogReceived;
        await _watchdog.StartAsync(CancellationToken.None);
        SetMonitoringState(true);
    }

    private async void StopButton_Click(object? sender, EventArgs e)
    {
        if (_watchdog is null)
        {
            return;
        }

        await _watchdog.StopAsync();
        SetMonitoringState(false);
    }

    private async void CheckNowButton_Click(object? sender, EventArgs e)
    {
        if (_watchdog is null)
        {
            if (!TryCreateOptions(out var options))
            {
                return;
            }

            _watchdog = new ProcessWatchdog(new SystemProcessController(), options);
            _watchdog.LogReceived += Watchdog_LogReceived;
        }

        await _watchdog.CheckNowAsync(CancellationToken.None);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择要监控的 EXE",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _exePathTextBox.Text = dialog.FileName;
        }
    }

    private void Watchdog_LogReceived(object? sender, WatchdogLogEntry e)
    {
        var level = GetLogLevel(e.Message);

        if (InvokeRequired)
        {
            BeginInvoke(() => AddLog(level, e.Message, e.Timestamp));
            return;
        }

        AddLog(level, e.Message, e.Timestamp);
    }

    private bool TryCreateOptions(out WatchdogOptions options)
    {
        options = new WatchdogOptions(
            _exePathTextBox.Text.Trim(),
            TimeSpan.FromSeconds((double)_intervalSecondsInput.Value),
            TimeSpan.FromSeconds((double)_unresponsiveSecondsInput.Value));

        try
        {
            options.Validate();

            if (!File.Exists(options.ExecutablePath))
            {
                MessageBox.Show(this, "选择的 EXE 文件不存在。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            MessageBox.Show(this, ex.Message, "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private void SetMonitoringState(bool isMonitoring)
    {
        _startButton.Enabled = !isMonitoring;
        _stopButton.Enabled = isMonitoring;
        _exePathTextBox.Enabled = !isMonitoring;
        _browseButton.Enabled = !isMonitoring;
        _intervalSecondsInput.Enabled = !isMonitoring;
        _unresponsiveSecondsInput.Enabled = !isMonitoring;

        _statusValueLabel.Text = isMonitoring ? "监控中" : "未监控";
        _statusValueLabel.ForeColor = isMonitoring ? Color.FromArgb(24, 126, 84) : Color.FromArgb(178, 74, 61);
        _statusHintLabel.Text = isMonitoring ? "正在按间隔检查目标进程" : "等待选择目标进程";
    }

    private void AddLog(string level, string message)
    {
        AddLog(level, message, DateTimeOffset.Now);
    }

    private void AddLog(string level, string message, DateTimeOffset timestamp)
    {
        var item = new ListViewItem(timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        item.SubItems.Add(level);
        item.SubItems.Add(message);
        item.ForeColor = level switch
        {
            "错误" => Color.FromArgb(255, 178, 166),
            "恢复" => Color.FromArgb(128, 230, 174),
            "警告" => Color.FromArgb(255, 214, 128),
            _ => Color.FromArgb(226, 233, 243)
        };

        _logListView.Items.Add(item);

        while (_logListView.Items.Count > 500)
        {
            _logListView.Items.RemoveAt(0);
        }

        item.EnsureVisible();
        ResizeLogColumns();
    }

    private static string GetLogLevel(string message)
    {
        if (message.Contains("失败", StringComparison.Ordinal) ||
            message.Contains("错误", StringComparison.Ordinal))
        {
            return "错误";
        }

        if (message.Contains("无响应", StringComparison.Ordinal) ||
            message.Contains("退出", StringComparison.Ordinal) ||
            message.Contains("未发现", StringComparison.Ordinal))
        {
            return "警告";
        }

        if (message.Contains("启动", StringComparison.Ordinal) ||
            message.Contains("重启", StringComparison.Ordinal))
        {
            return "恢复";
        }

        return "信息";
    }

    private Control BuildLabeledNumberField(
        string label,
        NumericUpDown input,
        decimal value,
        decimal minimum,
        decimal maximum,
        string suffix)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(CreateFieldLabel(label), 0, 0);

        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Value = value;
        input.Dock = DockStyle.Left;
        input.Margin = new Padding(0, 5, 0, 5);
        input.Width = 96;
        panel.Controls.Add(input, 1, 0);

        var suffixLabel = new Label
        {
            Text = suffix,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(91, 103, 122)
        };
        panel.Controls.Add(suffixLabel, 2, 0);

        return panel;
    }

    private Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 55, 72)
        };
    }

    private void ConfigureBrowseButton(Button button)
    {
        button.BackColor = Color.FromArgb(247, 249, 252);
        button.ForeColor = Color.FromArgb(38, 53, 74);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(198, 207, 220);
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
    }

    private void ConfigurePrimaryButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 10, 0);
        button.BackColor = Color.FromArgb(38, 112, 214);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
    }

    private void ConfigureSecondaryButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 10, 0);
        button.BackColor = Color.White;
        button.ForeColor = Color.FromArgb(38, 53, 74);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(200, 209, 222);
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
    }

    private void ResizeLogColumns()
    {
        if (_logListView.Columns.Count < 3)
        {
            return;
        }

        var width = Math.Max(420, _logListView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        _logListView.Columns[0].Width = 168;
        _logListView.Columns[1].Width = 82;
        _logListView.Columns[2].Width = Math.Max(240, width - 250);
    }

    private void DrawRoundedPanel(object? sender, PaintEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(211, 219, 231));
        var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
        using var path = CreateRoundedRectanglePath(rect, 8);
        e.Graphics.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
