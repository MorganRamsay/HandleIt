using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace HandleIt
{
    public class FontService
    {
        private static FontService? _instance;
        private static readonly Lock Lock = new();
        private readonly ObservableCollection<FontFamily> _fontFamilies = [];
        private bool _isInitialized;

        public static FontService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        _instance ??= new FontService();
                    }
                }
                return _instance;
            }
        }

        public ReadOnlyObservableCollection<FontFamily> FontFamilies { get; }

        private FontService()
        {
            FontFamilies = new ReadOnlyObservableCollection<FontFamily>(_fontFamilies);
            InitializeFontFamilies();
        }

        private void InitializeFontFamilies()
        {
            if (_isInitialized) return;

            Task.Run(() =>
            {
                var fonts = Fonts.SystemFontFamilies
                    .OrderBy(f => f.Source)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var font in fonts)
                    {
                        _fontFamilies.Add(font);
                    }
                    _isInitialized = true;
                });
            });
        }
    }
}