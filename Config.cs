using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;

namespace HandleIt
{
    public sealed class Config : INotifyPropertyChanged
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            int nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);

        private static readonly string DefaultConfigFile = Path.Combine(
            Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty,
            "HandleIt.ini");

        private static Config? _instance;
        private bool _isInitialized = false;

        // General settings
        private bool _alwaysOnTop;
        private bool _lockPosition;

        // Theme settings
        private string _themeMode = "System";

        // Custom theme settings
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 12;
        private Color _backgroundColor = Colors.White;
        private Color _foregroundColor = Colors.Black;
        private double _windowWidth = 100;
        private double _windowHeight = 30;

        // Process settings
        private string _processName = "explorer.exe";
        private int _pollingRate = 5000;

        // Config settings
        private string _configFilePath = DefaultConfigFile;

        public static Config Instance => _instance ??= new Config();

        public string ConfigFilePath
        {
            get => _configFilePath;
            set
            {
                if (_configFilePath != value)
                {
                    _configFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop != value)
                {
                    _alwaysOnTop = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LockPosition
        {
            get => _lockPosition;
            set
            {
                if (_lockPosition != value)
                {
                    _lockPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ThemeMode
        {
            get => _themeMode;
            set
            {
                if (_themeMode != value)
                {
                    _themeMode = value;
                    OnPropertyChanged();
                    // Notify that custom theme related properties might be affected
                    OnPropertyChanged(nameof(IsCustomTheme));
                }
            }
        }

        public bool IsCustomTheme => _themeMode == "Custom";

        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public double WindowWidth
        {
            get => _windowWidth;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_windowWidth != value)
                {
                    _windowWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public double WindowHeight
        {
            get => _windowHeight;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_windowHeight != value)
                {
                    _windowHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProcessName
        {
            get => _processName;
            set
            {
                if (_processName != value)
                {
                    _processName = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PollingRate
        {
            get => _pollingRate;
            set
            {
                if (_pollingRate != value)
                {
                    _pollingRate = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private int _warningThreshold = 90; // Default to 90% warning

        public int WarningThreshold
        {
            get => _warningThreshold;
            set
            {
                if (_warningThreshold != value)
                {
                    _warningThreshold = value;
                    OnPropertyChanged(nameof(WarningThreshold));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadSettings()
        {
            if (_isInitialized)
                return;

            var buffer = new StringBuilder(1024);

            // General settings
            GetPrivateProfileString("General", "AlwaysOnTop", _alwaysOnTop.ToString(), buffer, buffer.Capacity, _configFilePath);
            _alwaysOnTop = bool.Parse(buffer.ToString());

            GetPrivateProfileString("General", "LockPosition", _lockPosition.ToString(), buffer, buffer.Capacity, _configFilePath);
            _lockPosition = bool.Parse(buffer.ToString());

            // Theme settings
            GetPrivateProfileString("Theme", "Mode", _themeMode, buffer, buffer.Capacity, _configFilePath);
            _themeMode = buffer.ToString();

            GetPrivateProfileString("Theme", "FontFamily", _fontFamily, buffer, buffer.Capacity, _configFilePath);
            _fontFamily = buffer.ToString();

            GetPrivateProfileString("Theme", "FontSize", _fontSize.ToString(CultureInfo.InvariantCulture), buffer, buffer.Capacity, _configFilePath);
            _fontSize = double.Parse(buffer.ToString());

            GetPrivateProfileString("Theme", "BackgroundColor", ColorToHex(_backgroundColor), buffer, buffer.Capacity, _configFilePath);
            _backgroundColor = HexToColor(buffer.ToString());

            GetPrivateProfileString("Theme", "ForegroundColor", ColorToHex(_foregroundColor), buffer, buffer.Capacity, _configFilePath);
            _foregroundColor = HexToColor(buffer.ToString());

            // Window settings
            GetPrivateProfileString("Window", "Width", _windowWidth.ToString(CultureInfo.InvariantCulture), buffer, buffer.Capacity, _configFilePath);
            _windowWidth = double.Parse(buffer.ToString());

            GetPrivateProfileString("Window", "Height", _windowHeight.ToString(CultureInfo.InvariantCulture), buffer, buffer.Capacity, _configFilePath);
            _windowHeight = double.Parse(buffer.ToString());

            // Process settings
            GetPrivateProfileString("Process", "Name", _processName, buffer, buffer.Capacity, _configFilePath);
            _processName = buffer.ToString();

            GetPrivateProfileString("Process", "PollingRate", _pollingRate.ToString(), buffer, buffer.Capacity, _configFilePath);
            _pollingRate = int.Parse(buffer.ToString());
            
            var thresholdStr = new StringBuilder(255);
            GetPrivateProfileString("Process", "WarningThreshold", "90", thresholdStr, 255, ConfigFilePath);
            if (int.TryParse(thresholdStr.ToString(), out var threshold))
            {
                WarningThreshold = threshold;
            }

            _isInitialized = true;
        }

        public void SaveSettings()
        {
            // General settings
            WritePrivateProfileString("General", "AlwaysOnTop", _alwaysOnTop.ToString(), _configFilePath);
            WritePrivateProfileString("General", "LockPosition", _lockPosition.ToString(), _configFilePath);

            // Theme settings
            WritePrivateProfileString("Theme", "Mode", _themeMode, _configFilePath);
            WritePrivateProfileString("Theme", "FontFamily", _fontFamily, _configFilePath);
            WritePrivateProfileString("Theme", "FontSize", _fontSize.ToString(CultureInfo.InvariantCulture), _configFilePath);
            WritePrivateProfileString("Theme", "BackgroundColor", ColorToHex(_backgroundColor), _configFilePath);
            WritePrivateProfileString("Theme", "ForegroundColor", ColorToHex(_foregroundColor), _configFilePath);

            // Window settings
            WritePrivateProfileString("Window", "Width", _windowWidth.ToString(CultureInfo.InvariantCulture), _configFilePath);
            WritePrivateProfileString("Window", "Height", _windowHeight.ToString(CultureInfo.InvariantCulture), _configFilePath);

            // Process settings
            WritePrivateProfileString("Process", "Name", _processName, _configFilePath);
            WritePrivateProfileString("Process", "PollingRate", _pollingRate.ToString(), _configFilePath);
            WritePrivateProfileString("Process", "WarningThreshold", WarningThreshold.ToString(CultureInfo.InvariantCulture), ConfigFilePath);
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static Color HexToColor(string hex)
        {
            try
            {
                hex = hex.Replace("#", "");
                if (hex.Length == 6)
                {
                    var r = Convert.ToByte(hex[..2], 16);
                    var g = Convert.ToByte(hex.Substring(2, 2), 16);
                    var b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
            }
            catch
            {
                // Return white on error
                return Colors.White;
            }

            return Colors.White;
        }
    }
}