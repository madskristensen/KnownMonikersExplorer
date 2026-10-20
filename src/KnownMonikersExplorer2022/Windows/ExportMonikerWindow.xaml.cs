using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Community.VisualStudio.Toolkit;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer.Windows
{
    public partial class ExportMonikerWindow : Window
    {
        internal const int MaximumExportSize = 4096;
        private readonly KnownMonikersViewModel _model;
        private bool _isExporting;

        public ExportMonikerWindow(KnownMonikersViewModel model)
        {
            _model = model;
            InitializeComponent();
            txtSizeHint.Text = $"Enter a width from 1 to {MaximumExportSize} pixels.";
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OnLoadedAsync().FireAndForget();
        }

        private async Task OnLoadedAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Icon = await ImageMonikerBitmapCache.GetBitmapAsync(KnownMonikers.Export, 16);
                imgMoniker.Moniker = _model.Moniker;

                // Check if VS is currently using a dark theme
                System.Drawing.Color currentBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                chkDarkTheme.IsChecked = IsDarkColor(currentBackground);
                UpdatePreviewBackground();

                txtSize.Focus();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private static bool IsDarkColor(System.Drawing.Color color)
        {
            // Calculate perceived brightness using standard formula
            double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return brightness < 0.5;
        }

        private System.Drawing.Color GetSelectedBackgroundColor()
        {
            return chkDarkTheme.IsChecked == true 
                ? System.Drawing.Color.FromArgb(255, 37, 37, 38)  // Dark theme background
                : System.Drawing.Color.FromArgb(255, 246, 246, 246); // Light theme background
        }

        private void ChkDarkTheme_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePreviewBackground();
        }

        private void UpdatePreviewBackground()
        {
            var bgColor = GetSelectedBackgroundColor();
            var wpfColor = System.Windows.Media.Color.FromArgb(bgColor.A, bgColor.R, bgColor.G, bgColor.B);
            imgMoniker.SetValue(ImageThemingUtilities.ImageBackgroundColorProperty, wpfColor);
            imgBorder.Background = new SolidColorBrush(wpfColor);
        }

        private void TxtSize_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numeric input
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void TxtSize_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnOk.IsEnabled = !_isExporting && TryGetExportSize(txtSize.Text, out _);
        }

        internal static bool TryGetExportSize(string text, out int size)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out size)
                && size >= 1 && size <= MaximumExportSize;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (_isExporting)
            {
                return;
            }

            if (!TryGetExportSize(txtSize.Text, out var size))
            {
                txtError.Text = txtSizeHint.Text;
                txtSize.Focus();
                txtSize.SelectAll();
                return;
            }

            ExportAsync(size).FireAndForget();
        }

        private async Task ExportAsync(int size)
        {
            _isExporting = true;
            btnOk.IsEnabled = false;
            txtError.Text = string.Empty;

            try
            {
                // Get the selected theme's background color for proper image theming
                System.Drawing.Color backgroundColor = GetSelectedBackgroundColor();
                BitmapSource image = await _model.Moniker.ToBitmapSourceAsync(size, backgroundColor);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var saved = await SaveImageAsync(image, _model.Name);
                if (saved)
                {
                    Close();
                }
            }
            catch (NotSupportedException ex)
            {
                ex.Log();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                txtError.Text = "Unable to export the image. Choose a .png, .jpg, .jpeg, or .gif file. See the Output window for details.";
            }
            catch (Exception ex)
            {
                ex.Log();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                txtError.Text = "Unable to export the image. Check the destination and try again. See the Output window for details.";
            }
            finally
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _isExporting = false;
                btnOk.IsEnabled = TryGetExportSize(txtSize.Text, out _);
            }
        }

        private async Task<bool> SaveImageAsync(BitmapSource image, string name)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var sfd = new SaveFileDialog
            {
                FileName = name + ".png",
                DefaultExt = ".png",
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Gif Image|*.gif",
                Title = Title,
            };

            if (sfd.ShowDialog(this) == true)
            {
                SaveBitmapToDisk(image, sfd.FileName);
                await OptimizeImageAsync(sfd.FileName);
                return true;
            }

            return false;
        }

        private static void SaveBitmapToDisk(BitmapSource image, string fileName)
        {
            BitmapEncoder encoder = GetEncoder(fileName);
            var fileParentPath = Path.GetDirectoryName(fileName);

            if (Directory.Exists(fileParentPath) == false)
            {
                Directory.CreateDirectory(fileParentPath);
            }

            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }

        private static async Task OptimizeImageAsync(string fileName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                await VS.Commands.ExecuteAsync("ImageOptimizer.OptimizeLossless", fileName);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        internal static BitmapEncoder GetEncoder(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    return new JpegBitmapEncoder();
                case ".png":
                    return new PngBitmapEncoder();
                case ".gif":
                    return new GifBitmapEncoder();
                default:
                    throw new NotSupportedException("Only PNG, JPEG, and GIF image files are supported.");
            }
        }

        public class ViewModel
        {
            public ViewModel(string label, ImageMoniker moniker)
            {
                Label = label;
                Moniker = moniker;
            }

            public string Label { get; set; }

            public ImageMoniker Moniker { get; set; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}
