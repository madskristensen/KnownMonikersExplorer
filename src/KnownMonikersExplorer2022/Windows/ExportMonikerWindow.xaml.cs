using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
        private readonly KnownMonikersViewModel _model;

        public ExportMonikerWindow(KnownMonikersViewModel model)
        {
            _model = model;
            InitializeComponent();
            Loaded += OnLoadedAsync;
        }

        private async void OnLoadedAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Icon = await ImageMonikerBitmapCache.GetBitmapAsync(KnownMonikers.Export, 16);
                imgMoniker.Moniker = _model.Moniker;
                txtSize.Focus();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private async void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtSize.Text, out var size))
            {
                txtSize.Focus();
                txtSize.SelectAll();
                return;
            }

            try
            {
                // Get the current theme's background color for proper image theming
                System.Drawing.Color backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                BitmapSource image = await _model.Moniker.ToBitmapSourceAsync(size, backgroundColor);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var saved = await SaveImageAsync(image, _model.Name);
                if (saved)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                ex.Log();
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

            if (sfd.ShowDialog() == true)
            {
                SaveBitmapToDisk(image, sfd.FileName);
                await OptimizeImageAsync(sfd.FileName);
                return true;
            }

            return false;
        }

        private static void SaveBitmapToDisk(BitmapSource image, string fileName)
        {
            var fileParentPath = Path.GetDirectoryName(fileName);

            if (Directory.Exists(fileParentPath) == false)
            {
                Directory.CreateDirectory(fileParentPath);
            }

            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                BitmapEncoder encoder = GetEncoder(fileName);
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

        private static BitmapEncoder GetEncoder(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    return new JpegBitmapEncoder();
                case ".png":
                    return new PngBitmapEncoder();
            }

            return new GifBitmapEncoder();
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
