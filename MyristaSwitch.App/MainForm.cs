namespace MyristaSwitch.App;

internal sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly UsbDevicePoller _devicePoller = new();
    private readonly DisplayProfileService _displayProfileService = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly System.Windows.Forms.Timer _deviceChangeDebounceTimer = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly TextBox _keyboardFilter = new();
    private readonly TextBox _mouseFilter = new();
    private readonly ComboBox _keyboardCombo = new();
    private readonly ComboBox _mouseCombo = new();
    private readonly ComboBox _connectedActionCombo = new();
    private readonly ComboBox _disconnectedActionCombo = new();
    private readonly CheckBox _enabledCheck = new();
    private readonly CheckBox _requireBothCheck = new();
    private readonly CheckBox _startWithWindowsCheck = new();
    private readonly Label _stateValue = new();
    private readonly Label _displayValue = new();
    private readonly Label _lastEventValue = new();
    private readonly Label _statusLabel = new();
    private readonly Button _refreshButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _restoreButton = new();
    private readonly Button _autoDetectButton = new();
    private readonly Button _testConnectedButton = new();
    private readonly Button _testDisconnectedButton = new();
    private ThemePalette _theme;
    private IReadOnlyList<UsbDevice> _devices = [];
    private IReadOnlyList<UsbDevice> _autoDetectBaseline = [];
    private bool? _lastActiveState;
    private bool _polling;
    private bool _autoDetectActive;
    private const int WmDeviceChange = 0x0219;

    public MainForm(string[] args)
    {
        _settings = AppSettings.Load();
        _theme = BrandAssets.CurrentTheme;
        Icon = BrandAssets.CreateIcon();
        _notifyIcon = BuildNotifyIcon();
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.RestoreRequested += HotkeyWindowOnRestoreRequested;

        Text = "MyristaSwitch";
        MinimumSize = new Size(760, 560);
        Size = new Size(820, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        ApplyTheme();
        LoadSettingsIntoControls();

        _timer.Interval = 1000;
        _timer.Tick += async (_, _) => await PollAndApplyAsync();
        _deviceChangeDebounceTimer.Interval = 600;
        _deviceChangeDebounceTimer.Tick += async (_, _) =>
        {
            _deviceChangeDebounceTimer.Stop();
            await PollAndApplyAsync();
        };
        Shown += async (_, _) =>
        {
            await RefreshDevicesAsync();
            if (args.Any(arg => arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase)))
            {
                BeginInvoke(Hide);
            }
        };
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        };

        _timer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _deviceChangeDebounceTimer.Dispose();
            _notifyIcon.Dispose();
            _hotkeyWindow.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmDeviceChange)
        {
            _deviceChangeDebounceTimer.Stop();
            _deviceChangeDebounceTimer.Start();
        }

        base.WndProc(ref m);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 214));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildStatusStrip(), 0, 1);
        root.Controls.Add(BuildDeviceAndActionGrid(), 0, 2);
        root.Controls.Add(BuildOptionsGrid(), 0, 3);
        root.Controls.Add(BuildCommandBar(), 0, 4);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        header.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = BrandAssets.CreateLogoBitmap(42),
            SizeMode = PictureBoxSizeMode.CenterImage
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "MyristaSwitch\r\nKMS-aware display switching",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            AutoSize = false
        }, 1, 0);

        return header;
    }

    private Control BuildStatusStrip()
    {
        var strip = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));

        strip.Controls.Add(BuildMetric("KMS", _stateValue), 0, 0);
        strip.Controls.Add(BuildMetric("Displays", _displayValue), 1, 0);
        strip.Controls.Add(BuildMetric("Last event", _lastEventValue), 2, 0);
        return strip;
    }

    private Control BuildDeviceAndActionGrid()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        AddFilterField(grid, "Keyboard", _keyboardFilter, _keyboardCombo, 0);
        AddFilterField(grid, "Mouse / HID", _mouseFilter, _mouseCombo, 1);
        AddField(grid, "When KMS is here", _connectedActionCombo, 0, 3);
        AddField(grid, "When KMS leaves", _disconnectedActionCombo, 1, 3);
        return grid;
    }

    private Control BuildOptionsGrid()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 3, Height = 118 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _enabledCheck.Text = "Enable";
        _requireBothCheck.Text = "Require both mouse and keyboard";
        _startWithWindowsCheck.Text = "Launch at login";

        grid.Controls.Add(_enabledCheck, 0, 0);
        grid.Controls.Add(_requireBothCheck, 1, 0);
        grid.Controls.Add(_startWithWindowsCheck, 0, 1);
        grid.Controls.Add(_statusLabel, 0, 2);
        grid.SetColumnSpan(_statusLabel, 2);

        return grid;
    }

    private Control BuildCommandBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };

        ConfigureButton(_saveButton, "Save settings", true);
        _saveButton.Click += (_, _) => SaveSettingsFromControls();

        ConfigureButton(_restoreButton, "Restore + pause");
        _restoreButton.Click += async (_, _) => await RestoreExtendedAndPauseAsync();

        ConfigureButton(_autoDetectButton, "Auto detect");
        _autoDetectButton.Click += async (_, _) => await StartAutoDetectAsync();

        ConfigureButton(_refreshButton, "Refresh");
        _refreshButton.Click += async (_, _) => await RefreshDevicesAsync();

        ConfigureButton(_testConnectedButton, "Test here");
        _testConnectedButton.Click += async (_, _) => await ApplySelectedActionAsync((ScreenAction)_connectedActionCombo.SelectedItem!, "Connected action tested.");

        ConfigureButton(_testDisconnectedButton, "Test away");
        _testDisconnectedButton.Click += async (_, _) => await ApplySelectedActionAsync((ScreenAction)_disconnectedActionCombo.SelectedItem!, "Disconnected action tested.");

        bar.Controls.Add(_saveButton);
        bar.Controls.Add(_restoreButton);
        bar.Controls.Add(_autoDetectButton);
        bar.Controls.Add(_refreshButton);
        bar.Controls.Add(_testDisconnectedButton);
        bar.Controls.Add(_testConnectedButton);
        return bar;
    }

    private void ApplyTheme()
    {
        _theme = BrandAssets.CurrentTheme;
        BackColor = _theme.BackColor;
        ApplyThemeRecursive(this);
    }

    private void ApplyThemeRecursive(Control control)
    {
        if (control is Button button)
        {
            button.BackColor = button == _saveButton ? BrandAssets.Accent : _theme.ButtonColor;
            button.ForeColor = button == _saveButton ? Color.White : _theme.ButtonTextColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _theme.BorderColor;
            button.FlatAppearance.BorderSize = 1;
        }
        else if (control is ComboBox or TextBox)
        {
            control.BackColor = _theme.SurfaceColor;
            control.ForeColor = _theme.TextColor;
        }
        else if (control is Label label)
        {
            label.ForeColor = label == _statusLabel ? _theme.MutedTextColor : _theme.TextColor;
            label.BackColor = Color.Transparent;
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.ForeColor = _theme.TextColor;
            checkBox.BackColor = Color.Transparent;
        }
        else if (control is TableLayoutPanel or FlowLayoutPanel)
        {
            control.BackColor = _theme.BackColor;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeRecursive(child);
        }
    }

    private static void AddField(TableLayoutPanel grid, string label, ComboBox comboBox, int column, int row)
    {
        comboBox.Dock = DockStyle.Fill;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Margin = new Padding(0, 0, column == 0 ? 12 : 0, 8);
        grid.Controls.Add(NewFieldLabel(label), column, row);
        grid.Controls.Add(comboBox, column, row + 1);
    }

    private void AddFilterField(TableLayoutPanel grid, string label, TextBox filter, ComboBox comboBox, int column)
    {
        filter.Dock = DockStyle.Fill;
        filter.Margin = new Padding(0, 0, column == 0 ? 12 : 0, 6);
        filter.PlaceholderText = "Filter";
        filter.TextChanged += (_, _) => BindDeviceCombosPreservingSelection();

        comboBox.Dock = DockStyle.Fill;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Margin = new Padding(0, 0, column == 0 ? 12 : 0, 8);

        grid.Controls.Add(NewFieldLabel(label), column, 0);
        grid.Controls.Add(filter, column, 1);
        grid.Controls.Add(comboBox, column, 2);
    }

    private static Control BuildMetric(string label, Label value)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 0, 16, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.Controls.Add(NewFieldLabel(label), 0, 0);

        value.Dock = DockStyle.Fill;
        value.Text = "-";
        value.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        panel.Controls.Add(value, 0, 1);
        return panel;
    }

    private static Label NewFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
    }

    private void ConfigureButton(Button button, string text, bool primary = false)
    {
        button.Text = text;
        button.Width = primary ? 124 : 104;
        button.Height = 34;
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
    }

    private NotifyIcon BuildNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Restore Extend + Pause", null, async (_, _) => await RestoreExtendedAndPauseAsync());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        });

        var notifyIcon = new NotifyIcon
        {
            Icon = BrandAssets.CreateIcon(),
            Text = "MyristaSwitch",
            Visible = true,
            ContextMenuStrip = menu
        };

        notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
        return notifyIcon;
    }

    private void LoadSettingsIntoControls()
    {
        _enabledCheck.Checked = _settings.AutomationEnabled;
        _requireBothCheck.Checked = _settings.RequireBothDevices;
        _startWithWindowsCheck.Checked = StartupService.IsEnabled() || _settings.StartWithWindows;
        BindActionCombo(_connectedActionCombo, _settings.ConnectedAction);
        BindActionCombo(_disconnectedActionCombo, _settings.DisconnectedAction);
        UpdateStatus("Waiting for device scan.");
    }

    private static void BindActionCombo(ComboBox comboBox, ScreenAction selectedAction)
    {
        comboBox.DataSource = Enum.GetValues<ScreenAction>().ToArray();
        comboBox.SelectedItem = selectedAction;
    }

    private async Task RefreshDevicesAsync()
    {
        ToggleButtons(false);
        try
        {
            _devices = await _devicePoller.GetPresentInputDevicesAsync(CancellationToken.None);
            BindDeviceCombosPreservingSelection();
            UpdateLiveState();
            UpdateStatus($"Found {_devices.Count(device => device.IsUsable)} usable input devices.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Device scan failed: {ex.Message}", BrandAssets.Danger);
        }
        finally
        {
            ToggleButtons(true);
        }
    }

    private void BindDeviceCombosPreservingSelection()
    {
        BindDeviceCombo(
            _keyboardCombo,
            _keyboardCombo.SelectedValue as string ?? _settings.KeyboardInstanceId,
            _keyboardFilter.Text,
            device => device.IsKeyboard);

        BindDeviceCombo(
            _mouseCombo,
            _mouseCombo.SelectedValue as string ?? _settings.MouseInstanceId,
            _mouseFilter.Text,
            device => device.IsMouseLike);
    }

    private void BindDeviceCombo(ComboBox comboBox, string? selectedInstanceId, string filter, Func<UsbDevice, bool> includeDevice)
    {
        var items = new List<DeviceComboItem> { new("(not selected)", null) };
        items.AddRange(_devices
            .Where(includeDevice)
            .Where(device => device.IsUsable)
            .Where(device => MatchesFilter(device, filter))
            .Select(device => new DeviceComboItem(device.DisplayName, device.InstanceId)));

        if (selectedInstanceId is not null && items.All(item => !string.Equals(item.InstanceId, selectedInstanceId, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(new DeviceComboItem($"Selected disconnected device [{selectedInstanceId}]", selectedInstanceId));
        }

        comboBox.DataSource = items;
        comboBox.DisplayMember = nameof(DeviceComboItem.Label);
        comboBox.ValueMember = nameof(DeviceComboItem.InstanceId);
        if (selectedInstanceId is not null)
        {
            comboBox.SelectedValue = selectedInstanceId;
        }
        if (comboBox.SelectedIndex < 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static bool MatchesFilter(UsbDevice device, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return device.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            device.InstanceId.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void SaveSettingsFromControls()
    {
        _settings.AutomationEnabled = _enabledCheck.Checked;
        _settings.KeyboardInstanceId = _keyboardCombo.SelectedValue as string;
        _settings.KeyboardSignature = FindDeviceById(_settings.KeyboardInstanceId)?.Signature ?? _settings.KeyboardSignature;
        _settings.MouseInstanceId = _mouseCombo.SelectedValue as string;
        _settings.MouseSignature = FindDeviceById(_settings.MouseInstanceId)?.Signature ?? _settings.MouseSignature;
        _settings.ConnectedAction = (ScreenAction)_connectedActionCombo.SelectedItem!;
        _settings.DisconnectedAction = (ScreenAction)_disconnectedActionCombo.SelectedItem!;
        _settings.RequireBothDevices = _requireBothCheck.Checked;
        _settings.StartWithWindows = _startWithWindowsCheck.Checked;
        _settings.StartMinimized = _settings.StartWithWindows;
        _settings.PollIntervalSeconds = 1;
        _settings.Save();
        StartupService.SetEnabled(_settings.StartWithWindows);
        _timer.Interval = 1000;
        _lastActiveState = null;
        UpdateLiveState();
        UpdateStatus("Settings saved.");
    }

    private async Task PollAndApplyAsync()
    {
        if (_polling)
        {
            return;
        }

        _polling = true;
        try
        {
            if (_autoDetectActive || _settings.AutomationEnabled)
            {
                _devices = await _devicePoller.GetPresentInputDevicesAsync(CancellationToken.None);
            }

            if (_autoDetectActive && TryCompleteAutoDetect())
            {
                return;
            }

            _displayValue.Text = $"{_displayProfileService.DisplayCount} detected";
            if (!_settings.AutomationEnabled)
            {
                UpdateLiveState();
                return;
            }

            var active = IsSelectedKmsSideActive();
            UpdateLiveState(active);
            UpdateStatus($"Monitoring. {GetSelectedDeviceSummary()}");
            if (_lastActiveState == active)
            {
                return;
            }

            _lastActiveState = active;
            var action = active ? _settings.ConnectedAction : _settings.DisconnectedAction;
            await ApplySelectedActionAsync(action, active ? $"KMS connected. Applied {action}. {GetSelectedDeviceSummary()}" : $"KMS disconnected. Applied {action}. {GetSelectedDeviceSummary()}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Automation failed: {ex.Message}", BrandAssets.Danger);
        }
        finally
        {
            _polling = false;
        }
    }

    private async Task ApplySelectedActionAsync(ScreenAction action, string successMessage)
    {
        if (!_displayProfileService.CanSafelyRun(action))
        {
            UpdateStatus($"Skipped {action}: Windows reports only one display.", BrandAssets.Warn);
            return;
        }

        await _displayProfileService.ApplyAsync(action, CancellationToken.None);
        _lastEventValue.Text = DateTime.Now.ToString("HH:mm:ss");
        _displayValue.Text = $"{_displayProfileService.DisplayCount} detected";
        UpdateStatus(successMessage);
    }

    private bool IsSelectedKmsSideActive()
    {
        var selectedIds = new[] { _settings.KeyboardInstanceId, _settings.MouseInstanceId }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        if (selectedIds.Length == 0)
        {
            return false;
        }

        var activeStates = new[]
        {
            IsSelectedDeviceUsable(_settings.KeyboardInstanceId, _settings.KeyboardSignature),
            IsSelectedDeviceUsable(_settings.MouseInstanceId, _settings.MouseSignature)
        }.Where(state => state.HasValue).Select(state => state!.Value).ToArray();

        if (activeStates.Length == 0)
        {
            return false;
        }

        return _settings.RequireBothDevices
            ? activeStates.All(active => active)
            : activeStates.Any(active => active);
    }

    private bool? IsSelectedDeviceUsable(string? instanceId, string? signature)
    {
        if (string.IsNullOrWhiteSpace(instanceId) && string.IsNullOrWhiteSpace(signature))
        {
            return null;
        }

        var exact = FindDeviceById(instanceId);
        if (exact is not null)
        {
            return exact.IsUsable;
        }

        if (!string.IsNullOrWhiteSpace(signature))
        {
            var signatureMatch = _devices.FirstOrDefault(device =>
                device.IsUsable &&
                string.Equals(device.Signature, signature, StringComparison.OrdinalIgnoreCase));
            return signatureMatch is not null;
        }

        return false;
    }

    private string GetSelectedDeviceSummary()
    {
        var keyboard = IsSelectedDeviceUsable(_settings.KeyboardInstanceId, _settings.KeyboardSignature);
        var mouse = IsSelectedDeviceUsable(_settings.MouseInstanceId, _settings.MouseSignature);
        return $"Keyboard: {FormatDeviceState(keyboard)}, Mouse: {FormatDeviceState(mouse)}";
    }

    private static string FormatDeviceState(bool? state)
    {
        return state switch
        {
            true => "OK",
            false => "Missing",
            null => "Not selected"
        };
    }

    private UsbDevice? FindDeviceById(string? instanceId)
    {
        return string.IsNullOrWhiteSpace(instanceId)
            ? null
            : _devices.FirstOrDefault(device => string.Equals(device.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task StartAutoDetectAsync()
    {
        ToggleButtons(false);
        try
        {
            _autoDetectBaseline = (await _devicePoller.GetPresentInputDevicesAsync(CancellationToken.None))
                .Where(device => device.IsUsable)
                .ToList();
            _devices = _autoDetectBaseline;
            BindDeviceCombosPreservingSelection();
            _autoDetectActive = true;
            _autoDetectButton.Text = "Detecting...";
            UpdateStatus("Auto detect armed. Press the physical KMS button now.");
        }
        catch (Exception ex)
        {
            _autoDetectActive = false;
            UpdateStatus($"Auto detect failed to start: {ex.Message}", BrandAssets.Danger);
        }
        finally
        {
            ToggleButtons(true);
            _autoDetectButton.Enabled = !_autoDetectActive;
        }
    }

    private bool TryCompleteAutoDetect()
    {
        var presentIds = _devices
            .Where(device => device.IsUsable)
            .Select(device => device.InstanceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = _autoDetectBaseline
            .Where(device => !presentIds.Contains(device.InstanceId))
            .ToList();

        var keyboard = missing.FirstOrDefault(device => device.IsKeyboard);
        var mouse = missing.FirstOrDefault(device => device.ClassName.Equals("Mouse", StringComparison.OrdinalIgnoreCase))
            ?? missing.FirstOrDefault(device => device.IsMouseLike && device != keyboard);

        if (keyboard is null && mouse is null)
        {
            return false;
        }

        if (keyboard is not null)
        {
            _settings.KeyboardInstanceId = keyboard.InstanceId;
            _settings.KeyboardSignature = keyboard.Signature;
            _keyboardFilter.Clear();
        }

        if (mouse is not null)
        {
            _settings.MouseInstanceId = mouse.InstanceId;
            _settings.MouseSignature = mouse.Signature;
            _mouseFilter.Clear();
        }

        _settings.RequireBothDevices = keyboard is not null && mouse is not null;
        _requireBothCheck.Checked = _settings.RequireBothDevices;
        _autoDetectActive = false;
        _autoDetectBaseline = [];
        _autoDetectButton.Text = "Auto detect";
        _autoDetectButton.Enabled = true;
        BindDeviceCombosPreservingSelection();
        SaveSettingsFromControls();

        UpdateStatus(keyboard is not null && mouse is not null
            ? "Auto detect selected the disconnected keyboard and mouse."
            : "Auto detect selected one disconnected device. Review the selection before enabling automation.", keyboard is not null && mouse is not null ? null : BrandAssets.Warn);
        return true;
    }

    private void UpdateLiveState(bool? active = null)
    {
        var isActive = active ?? IsSelectedKmsSideActive();
        _stateValue.Text = isActive ? "Here" : "Away";
        _stateValue.ForeColor = isActive ? BrandAssets.PrimaryDark : BrandAssets.Warn;
        _displayValue.Text = $"{_displayProfileService.DisplayCount} detected";
        if (_lastEventValue.Text == "-")
        {
            _lastEventValue.Text = "No switch yet";
        }
    }

    private async void HotkeyWindowOnRestoreRequested(object? sender, EventArgs e)
    {
        await RestoreExtendedAndPauseAsync();
    }

    private async Task RestoreExtendedAndPauseAsync()
    {
        _enabledCheck.Checked = false;
        _settings.AutomationEnabled = false;
        _settings.Save();
        await _displayProfileService.ApplyAsync(ScreenAction.Extend, CancellationToken.None);
        _lastEventValue.Text = DateTime.Now.ToString("HH:mm:ss");
        UpdateLiveState();
        UpdateStatus("Emergency restore applied. Automation paused.");
    }

    private void ShowMainWindow()
    {
        ApplyTheme();
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ToggleButtons(bool enabled)
    {
        _refreshButton.Enabled = enabled;
        _saveButton.Enabled = enabled;
        _restoreButton.Enabled = enabled;
        _autoDetectButton.Enabled = enabled && !_autoDetectActive;
        _testConnectedButton.Enabled = enabled;
        _testDisconnectedButton.Enabled = enabled;
    }

    private void UpdateStatus(string message, Color? color = null)
    {
        _statusLabel.ForeColor = color ?? _theme.MutedTextColor;
        _statusLabel.Text = $"{message}  Emergency: Ctrl+Alt+Shift+M";
    }

    private sealed record DeviceComboItem(string Label, string? InstanceId);
}
