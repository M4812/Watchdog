using Watchdog.Core;

namespace Watchdog.App;

public sealed class MainForm : Form
{
    private readonly TextBox _exePathTextBox = new();
    private readonly TextBox _folderPathTextBox = new();
    private readonly CheckBox _enableFolderWatchCheckBox = new();
    private readonly NumericUpDown _intervalSecondsInput = new();
    private readonly NumericUpDown _unresponsiveSecondsInput = new();
    private readonly NumericUpDown _folderBacklogSecondsInput = new();
    private readonly Button _browseButton = new();
    private readonly Button _browseFolderButton = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _checkNowButton = new();
    private readonly Label _statusValueLabel = new();
    private readonly ListView _logListView = new();
    private readonly PictureBox _dogPictureBox = new();
    private readonly Panel _brandPanel = new();
    private readonly Panel _settingsPanel = new();
    private readonly Panel _statusPanel = new();
    private readonly Panel _logPanel = new();
    private readonly Label _lockHintLabel = new();
    private readonly WatchdogFileLogger _fileLogger = new(Path.Combine(AppContext.BaseDirectory, "log"));

    private ProcessWatchdog? _watchdog;

    public MainForm()
    {
        InitializeWindow();
        BuildLayout();
        SetMonitoringState(false);
        UpdateFolderWatchControls();
        AddLog("系统", "请选择要守护的 EXE；如需按文件堆积重启，也可以启用文件夹监控。");
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
        MinimumSize = new Size(880, 760);
        Size = new Size(900, 790);
        BackColor = Color.FromArgb(240, 244, 249);
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 24),
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 382));
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
            Padding = new Padding(0, 0, 0, 10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 182));
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
        panel.Padding = new Padding(16);
        panel.Paint += DrawRoundedPanel;

        _dogPictureBox.Dock = DockStyle.Fill;
        _dogPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        _dogPictureBox.Image = IconFactory.CreateDogImage(150);
        panel.Controls.Add(_dogPictureBox);
        return panel;
    }

    private Control BuildWorkPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(BuildHeader(), 0, 0);
        panel.Controls.Add(BuildSettingsPanel(), 0, 1);
        return panel;
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        var titlePanel = panel;
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        titlePanel.Controls.Add(new Label
        {
            Text = "进程监控配置",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 17F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 31, 42),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);

        titlePanel.Controls.Add(new Label
        {
            Text = "选择目标 EXE，按间隔检查并自动恢复异常进程。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(91, 103, 122),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        return panel;
    }

    private Control BuildSettingsPanel()
    {
        var panel = _settingsPanel;
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.White;
        panel.Padding = new Padding(20, 14, 20, 14);
        panel.Paint += DrawRoundedPanel;

        var surface = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(surface);
        surface.Resize += (_, _) => LayoutSettingsSurface(surface);

        AddFieldLabel(surface, "EXE 路径", 12, 8, 86, 26);
        _exePathTextBox.PlaceholderText = @"例如：C:\Apps\Server.exe";
        surface.Controls.Add(_exePathTextBox);
        ConfigureBrowseButton(_browseButton);
        _browseButton.Text = "选择 EXE";
        _browseButton.Click += BrowseButton_Click;
        surface.Controls.Add(_browseButton);

        _enableFolderWatchCheckBox.Text = "启用文件夹堆积监控";
        _enableFolderWatchCheckBox.ForeColor = Color.FromArgb(45, 55, 72);
        _enableFolderWatchCheckBox.CheckedChanged += (_, _) => UpdateFolderWatchControls();
        surface.Controls.Add(_enableFolderWatchCheckBox);

        AddFieldLabel(surface, "堆积文件夹", 12, 76, 86, 26);
        _folderPathTextBox.PlaceholderText = @"可选，例如：C:\Apps\Queue";
        surface.Controls.Add(_folderPathTextBox);
        ConfigureBrowseButton(_browseFolderButton);
        _browseFolderButton.Text = "选择文件夹";
        _browseFolderButton.Click += BrowseFolderButton_Click;
        surface.Controls.Add(_browseFolderButton);

        AddFieldLabel(surface, "检查间隔", 12, 112, 86, 26);
        ConfigureNumberInput(_intervalSecondsInput, 10, 1, 3600);
        surface.Controls.Add(_intervalSecondsInput);
        AddUnitLabel(surface, "秒", 196, 112, 80, 26);

        AddFieldLabel(surface, "无响应判定", 12, 148, 96, 26);
        ConfigureNumberInput(_unresponsiveSecondsInput, 5, 1, 300);
        surface.Controls.Add(_unresponsiveSecondsInput);
        AddUnitLabel(surface, "秒后重启", 196, 148, 110, 26);

        AddFieldLabel(surface, "堆积判定", 12, 184, 86, 26);
        ConfigureNumberInput(_folderBacklogSecondsInput, 60, 1, 86400);
        surface.Controls.Add(_folderBacklogSecondsInput);
        AddUnitLabel(surface, "秒内仍有文件则重启", 196, 184, 190, 26);

        _statusPanel.BackColor = Color.FromArgb(255, 250, 247);
        _statusPanel.Padding = new Padding(8, 0, 8, 0);
        _statusPanel.Paint += DrawRoundedPanel;
        _statusValueLabel.Dock = DockStyle.Fill;
        _statusValueLabel.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
        _statusValueLabel.TextAlign = ContentAlignment.MiddleCenter;
        _statusPanel.Controls.Add(_statusValueLabel);
        surface.Controls.Add(_statusPanel);

        ConfigurePrimaryButton(_startButton, "开始监控");
        _startButton.Click += StartButton_Click;
        surface.Controls.Add(_startButton);

        ConfigureSecondaryButton(_stopButton, "停止监控");
        _stopButton.Click += StopButton_Click;
        surface.Controls.Add(_stopButton);

        ConfigureSecondaryButton(_checkNowButton, "立即检查");
        _checkNowButton.Click += CheckNowButton_Click;
        surface.Controls.Add(_checkNowButton);

        _lockHintLabel.Text = "启动后锁定配置，停止后可修改。";
        _lockHintLabel.ForeColor = Color.FromArgb(91, 103, 122);
        _lockHintLabel.TextAlign = ContentAlignment.MiddleRight;
        surface.Controls.Add(_lockHintLabel);
        return panel;
    }

    private Control BuildLogPanel()
    {
        var panel = _logPanel;
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.White;
        panel.Padding = new Padding(18);
        panel.Paint += DrawRoundedPanel;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(layout);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.Controls.Add(header, 0, 0);

        header.Controls.Add(new Label
        {
            Text = "运行日志",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 31, 42),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text = "最多保留 500 条",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(91, 103, 122),
            TextAlign = ContentAlignment.MiddleRight
        }, 1, 0);

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

    private void BrowseFolderButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择需要监视文件堆积的文件夹"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void Watchdog_LogReceived(object? sender, WatchdogLogEntry e)
    {
        _ = _fileLogger.WriteIfRecoveryEventAsync(e.Message, CancellationToken.None);

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
        if (_enableFolderWatchCheckBox.Checked && string.IsNullOrWhiteSpace(_folderPathTextBox.Text))
        {
            MessageBox.Show(this, "启用文件夹堆积监控后，请选择要监视的文件夹。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            options = new WatchdogOptions(string.Empty, TimeSpan.Zero, TimeSpan.Zero);
            return false;
        }

        var watchedFolderPath = _enableFolderWatchCheckBox.Checked ? _folderPathTextBox.Text.Trim() : null;

        options = new WatchdogOptions(
            _exePathTextBox.Text.Trim(),
            TimeSpan.FromSeconds((double)_intervalSecondsInput.Value),
            TimeSpan.FromSeconds((double)_unresponsiveSecondsInput.Value),
            watchedFolderPath: watchedFolderPath,
            folderBacklogTimeout: watchedFolderPath is null ? null : TimeSpan.FromSeconds((double)_folderBacklogSecondsInput.Value));

        try
        {
            options.Validate();

            if (!File.Exists(options.ExecutablePath))
            {
                MessageBox.Show(this, "选择的 EXE 文件不存在。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (options.WatchedFolderPath is not null && !Directory.Exists(options.WatchedFolderPath))
            {
                MessageBox.Show(this, "选择的堆积文件夹不存在。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        _enableFolderWatchCheckBox.Enabled = !isMonitoring;
        _browseButton.Enabled = !isMonitoring;
        _intervalSecondsInput.Enabled = !isMonitoring;
        _unresponsiveSecondsInput.Enabled = !isMonitoring;
        _folderBacklogSecondsInput.Enabled = !isMonitoring;
        UpdateFolderWatchControls();

        _statusValueLabel.Text = isMonitoring ? "监控中" : "未监控";
        _statusValueLabel.ForeColor = isMonitoring ? Color.FromArgb(24, 126, 84) : Color.FromArgb(178, 74, 61);
        _statusPanel.BackColor = isMonitoring ? Color.FromArgb(240, 253, 246) : Color.FromArgb(255, 250, 247);
        _statusPanel.Invalidate();
    }

    private void UpdateFolderWatchControls()
    {
        var isMonitoring = _watchdog?.IsMonitoring == true;
        var folderSelectionEnabled = _enableFolderWatchCheckBox.Checked && !isMonitoring;
        _folderPathTextBox.Enabled = folderSelectionEnabled;
        _browseFolderButton.Enabled = folderSelectionEnabled;
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
        if (ContainsOrdinal(message, "失败") ||
            ContainsOrdinal(message, "错误") ||
            ContainsOrdinal(message, "不存在"))
        {
            return "错误";
        }

        if (ContainsOrdinal(message, "无响应") ||
            ContainsOrdinal(message, "退出") ||
            ContainsOrdinal(message, "未发现") ||
            ContainsOrdinal(message, "堆积"))
        {
            return "警告";
        }

        if (ContainsOrdinal(message, "启动") ||
            ContainsOrdinal(message, "重启"))
        {
            return "恢复";
        }

        return "信息";
    }

    private static bool ContainsOrdinal(string value, string text)
    {
        return value.IndexOf(text, StringComparison.Ordinal) >= 0;
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

    private static void AddFieldLabel(Control parent, string text, int x, int y, int width, int height)
    {
        parent.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(parent.Font.FontFamily, 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 55, 72)
        });
    }

    private static void AddUnitLabel(Control parent, string text, int x, int y, int width, int height)
    {
        parent.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(91, 103, 122)
        });
    }

    private static void ConfigureNumberInput(NumericUpDown input, decimal value, decimal minimum, decimal maximum)
    {
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Value = value;
    }

    private void LayoutSettingsSurface(Control surface)
    {
        var rightButtonX = Math.Max(420, surface.ClientSize.Width - 148);
        var inputRight = rightButtonX - 14;
        var pathInputWidth = Math.Max(280, inputRight - 104);

        _exePathTextBox.SetBounds(104, 8, pathInputWidth, 26);
        _browseButton.SetBounds(rightButtonX, 8, 128, 26);

        _enableFolderWatchCheckBox.SetBounds(104, 40, 240, 26);

        _folderPathTextBox.SetBounds(104, 76, pathInputWidth, 26);
        _browseFolderButton.SetBounds(rightButtonX, 76, 128, 26);

        _intervalSecondsInput.SetBounds(104, 112, 78, 26);
        _unresponsiveSecondsInput.SetBounds(104, 148, 78, 26);
        _folderBacklogSecondsInput.SetBounds(104, 184, 78, 26);

        _statusPanel.SetBounds(Math.Max(0, surface.ClientSize.Width - 124), 184, 104, 28);
        _startButton.SetBounds(12, 216, 112, 30);
        _stopButton.SetBounds(136, 216, 112, 30);
        _checkNowButton.SetBounds(260, 216, 112, 30);
        _lockHintLabel.Text = "启动后锁定配置";
        _lockHintLabel.SetBounds(Math.Max(384, surface.ClientSize.Width - 180), 216, 160, 30);
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
        button.Dock = DockStyle.None;
        button.Margin = new Padding(0);
        button.BackColor = Color.FromArgb(38, 112, 214);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
    }

    private void ConfigureSecondaryButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.None;
        button.Margin = new Padding(0);
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
