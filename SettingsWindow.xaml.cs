using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;

namespace HandleIt
{
    public partial class SettingsWindow : Window
    {
        private bool _disposed;
        
        private readonly Config _config;
        private readonly Config _configCopy;

        public SettingsWindow()
        {
            InitializeComponent();

            // Get the config instance and create a working copy
            _config = Config.Instance;

            // Create a deep copy of the configuration for editing
            _configCopy = new Config
            {
                AlwaysOnTop = _config.AlwaysOnTop,
                LockPosition = _config.LockPosition,
                ThemeMode = _config.ThemeMode,
                FontFamily = _config.FontFamily,
                FontSize = _config.FontSize,
                BackgroundColor = _config.BackgroundColor,
                ForegroundColor = _config.ForegroundColor,
                WindowWidth = _config.WindowWidth,
                WindowHeight = _config.WindowHeight,
                ProcessName = _config.ProcessName,
                PollingRate = _config.PollingRate,
                ConfigFilePath = _config.ConfigFilePath,
                WarningThreshold = _config.WarningThreshold
            };

            // Set DataContext to the copy for editing
            DataContext = _configCopy;
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }
        
        public void Dispose()
        {
            if (_disposed) return;

            // Clear bindings and references
            DataContext = null;
        
            // Clear any event handlers if you have any
            // RemoveEventHandlers();

            _disposed = true;
        }

        private void ApplyAndClose_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings from the copy to the real config
            _config.AlwaysOnTop = _configCopy.AlwaysOnTop;
            _config.LockPosition = _configCopy.LockPosition;
            _config.ThemeMode = _configCopy.ThemeMode;
            _config.FontFamily = _configCopy.FontFamily;
            _config.FontSize = _configCopy.FontSize;
            _config.BackgroundColor = _configCopy.BackgroundColor;
            _config.ForegroundColor = _configCopy.ForegroundColor;
            _config.WindowWidth = _configCopy.WindowWidth;
            _config.WindowHeight = _configCopy.WindowHeight;
            _config.ProcessName = _configCopy.ProcessName;
            _config.PollingRate = _configCopy.PollingRate;
            _config.ConfigFilePath = _configCopy.ConfigFilePath;
            _config.WarningThreshold = _configCopy.WarningThreshold;

            _config.SaveSettings();
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void IncrementFontSize_Click(object sender, RoutedEventArgs e)
        {
            _configCopy.FontSize += 1;
        }

        private void DecrementFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (_configCopy.FontSize > 1)
            {
                _configCopy.FontSize -= 1;
            }
        }

        private void BackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement color picker dialog
            // For now, let's just show a message
            MessageBox.Show("Color picker not implemented yet. Please enter a hex color value in the text box.",
                "Color Picker", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ForegroundColor_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement color picker dialog
            // For now, let's just show a message
            MessageBox.Show("Color picker not implemented yet. Please enter a hex color value in the text box.",
                "Color Picker", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BrowseConfigFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                FileName = _configCopy.ConfigFilePath,
                Title = "Select Configuration File"
            };

            if (dialog.ShowDialog() == true)
            {
                _configCopy.ConfigFilePath = dialog.FileName;
            }
        }
    }

    // Converter for radio button binding
    public class RadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return (bool)value ? parameter.ToString() : value;
        }
    }

    // Converter for color to hex string
    public class ColorToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Color color)
            {
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            return "#FFFFFF";
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is string hexString)
            {
                try
                {
                    hexString = hexString.Replace("#", "");
                    if (hexString.Length == 6)
                    {
                        var r = System.Convert.ToByte(hexString[..2], 16);
                        var g = System.Convert.ToByte(hexString.Substring(2, 2), 16);
                        var b = System.Convert.ToByte(hexString.Substring(4, 2), 16);
                        return Color.FromRgb(r, g, b);
                    }
                }
                catch
                {
                    // Return white on error
                    return Colors.White;
                }
            }

            return Colors.White;
        }
    }
}