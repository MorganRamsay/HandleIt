using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HandleIt
{
    public partial class MainWindow
    {
        private Point? _dragStart;
        private Point _savedPosition;
        private const uint GdiHandleDefaultMax = 10000;
        private uint _maxGdiHandles;
        private bool _warningShown;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Config _config;

        // Windows API for GDI and INI
        [DllImport("user32.dll")]
        private static extern int GetGuiResources(IntPtr hProcess, uint uiFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileString(string section, string key, string def, StringBuilder retVal,
            int size, string filePath);

        private const uint ProcessQueryInformation = 0x0400;
        private const int SmCxscreen = 0;
        private const int SmCyscreen = 1;

        public MainWindow()
        {
            InitializeComponent();
            
            InitializeMaxGdiHandles();
            
            _ = FontService.Instance;

            // Load configuration
            _config = Config.Instance;
            _config.LoadSettings();
            _config.PropertyChanged += Config_PropertyChanged;

            SetupWindow();
            StartMonitoring();
        }

        private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update UI when config changes
            ApplySettings();
        }
        
        private void InitializeMaxGdiHandles()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows");
                if (key?.GetValue("GDIProcessHandleQuota") is int quota)
                {
                    _maxGdiHandles = (uint)quota;
                    return;
                }
            }
            catch
            {
                // Fallback to default if registry access fails
            }
            _maxGdiHandles = GdiHandleDefaultMax;
        }
        
        private void ShowWarningNotification(int currentHandles)
        {
            if (_warningShown) return;
        
            var threshold = (_maxGdiHandles * _config.WarningThreshold) / 100;
            if (currentHandles >= threshold)
            {
                _warningShown = true;
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        Application.Current.MainWindow,
                        $"Warning: GDI handle count ({currentHandles}) has reached {_config.WarningThreshold}% of maximum ({_maxGdiHandles}).\n\n" +
                        "Please save your work and restart the application to free up resources.",
                        "GDI Handle Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }
            else
            {
                _warningShown = false;
            }
        }


        private void ApplySettings()
        {
            // Apply window settings
            Topmost = _config.AlwaysOnTop;

            // Apply theme settings
            ApplyTheme();

            // Apply process settings
            // The monitoring thread will pick up the new process name and polling rate
        }

        private void ApplyTheme()
        {
            // Get the DPI scale to properly scale dimensions
            var dpiScale = GetDpiScale();

            var whiteBrush = Brushes.White;
            whiteBrush.Freeze();
            
            var blackBrush = Brushes.Black;
            blackBrush.Freeze();

            switch (_config.ThemeMode)
            {
                case "Light":
                    MainBorder.Background = whiteBrush;
                    HandleLabel.Foreground = blackBrush;
                    break;
                case "Dark":
                    MainBorder.Background = blackBrush;
                    HandleLabel.Foreground = whiteBrush;
                    break;
                case "System":
                    // For system theme, we would detect the OS theme
                    // For simplicity, we'll use light theme as default
                    MainBorder.Background = whiteBrush;
                    HandleLabel.Foreground = blackBrush;
                    break;
                case "Custom":
                    var bgBrush = new SolidColorBrush(_config.BackgroundColor);
                    bgBrush.Freeze();
                    MainBorder.Background = bgBrush;

                    var fgBrush = new SolidColorBrush(_config.ForegroundColor);
                    fgBrush.Freeze();
                    HandleLabel.Foreground = fgBrush;

                    HandleLabel.FontFamily = new FontFamily(_config.FontFamily);
                    HandleLabel.FontSize = _config.FontSize * dpiScale;
                    Width = _config.WindowWidth * dpiScale;
                    Height = _config.WindowHeight * dpiScale;
                    break;
            }
        }

        private void SetupWindow()
        {
            SetProcessDPIAware();

            // Apply settings from config
            ApplySettings();

            LoadPosition();

            MouseDown += Window_MouseDown;
            MouseMove += Window_MouseMove;
            MouseUp += Window_MouseUp;
            HandleLabel.MouseDown += Window_MouseDown;
            HandleLabel.MouseMove += Window_MouseMove;
            HandleLabel.MouseUp += Window_MouseUp;
        }

        private double GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }

        private void LoadPosition()
        {
            try
            {
                if (File.Exists(_config.ConfigFilePath))
                {
                    var xVal = new StringBuilder(255);
                    var yVal = new StringBuilder(255);
                    GetPrivateProfileString("Window", "X", "", xVal, 255, _config.ConfigFilePath);
                    GetPrivateProfileString("Window", "Y", "", yVal, 255, _config.ConfigFilePath);
                    if (double.TryParse(xVal.ToString(), out var ax) && double.TryParse(yVal.ToString(), out var ay))
                    {
                        Left = ax;
                        Top = ay;
                        _savedPosition = new Point(ax, ay);
                        return;
                    }
                }
            }
            catch
            {
                // Fallback to default position calculation
            }

            var screenWidth = GetSystemMetrics(SmCxscreen);
            var screenHeight = GetSystemMetrics(SmCyscreen);
            var taskbarHeight = 40; // Approximate
            var x = screenWidth - Width - 10;
            var y = screenHeight - Height - taskbarHeight - 10;
            Left = x;
            Top = y;
            _savedPosition = new Point(x, y);
        }

        private void SavePosition()
        {
            try
            {
                WritePrivateProfileString("Window", "X", _savedPosition.X.ToString(CultureInfo.InvariantCulture),
                    _config.ConfigFilePath);
                WritePrivateProfileString("Window", "Y", _savedPosition.Y.ToString(CultureInfo.InvariantCulture),
                    _config.ConfigFilePath);
            }
            catch
            {
                // Ignore errors when saving
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_config.LockPosition)
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _dragStart = e.GetPosition(null);
                CaptureMouse();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_config.LockPosition)
                return;

            if (_dragStart != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(null);
                Left += currentPosition.X - _dragStart.Value.X;
                Top += currentPosition.Y - _dragStart.Value.Y;
                _savedPosition = new Point(Left, Top);
                SavePosition();
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_config.LockPosition)
                return;

            if (e.LeftButton == MouseButtonState.Released)
            {
                _dragStart = null;
                ReleaseMouseCapture();
            }
        }

        private int? GetGdiHandles(int pid)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(ProcessQueryInformation, false, pid);
                if (hProcess != IntPtr.Zero)
                {
                    var handles = GetGuiResources(hProcess, 0);
                    return handles != 0 ? handles : null;
                }
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                }
            }
        }

        private int? GetPidByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                int? result = processes.Length > 0 ? processes[0].Id : null;
                Array.ForEach(processes, p => p.Dispose());
                return result;
            }
            catch
            {
                return null;
            }
        }

        private void StartMonitoring()
        {
            var thread = new Thread(() =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var processName = _config.ProcessName;
                        var pid = GetPidByName(processName);
                        var text = "Waiting for process...";
                        if (pid != null)
                        {
                            var handles = GetGdiHandles(pid.Value);
                            if (handles.HasValue)
                            {
                                text = handles.Value.ToString();
                                ShowWarningNotification(handles.Value);
                            }
                            else
                            {
                                text = "Waiting for process...";
                            }
                        }

                        Dispatcher.Invoke(() => HandleLabel.Content = text);

                        // Use the polling rate from config
                        Thread.Sleep(_config.PollingRate);
                    }
                })
                { IsBackground = true };
            thread.Start();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void ExitApplication()
        {
            if (ContextMenu != null) ContextMenu.IsOpen = false;
            _cts.Cancel();
            SavePosition();
            Application.Current.Shutdown();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
    }
}