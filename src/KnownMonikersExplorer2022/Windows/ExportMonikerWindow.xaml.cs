using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Community.VisualStudio.Toolkit;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer.Windows
{
    public partial class ExportMonikerWindow : Window
    {
        private static KnownMonikersViewModel _model;

        public ExportMonikerWindow(KnownMonikersViewModel model)
        {
            _model = model;
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                Icon = await KnownMonikers.Export.ToBitmapSourceAsync(16);
                imgMoniker.Moniker = _model.Moniker;

                txtSize.Focus();
            });
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSize.Text, out var size))
            {
                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    BitmapSource image = await _model.Moniker.ToBitmapSourceAsync(size);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var xaml = _model.GetXaml();

                    var saved = await SaveImageAsync(image, _model.Name, xaml);

                    if (saved)
                    {
                        Close();
                    }
                });
            }
            else
            {
                txtSize.Focus();
                txtSize.SelectAll();
            }
        }

        private async Task<bool> SaveImageAsync(BitmapSource image, string name, string xaml)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var sfd = new SaveFileDialog
            {
                FileName = name + ".png",
                DefaultExt = ".png",
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Gif Image|*.gif",
                Title = Title,
            };

            if (!String.IsNullOrEmpty(xaml))
            {
                sfd.Filter += "|Xaml|*.xaml";
            }

            if (sfd.ShowDialog() == true)
            {
                EnsureDirectory(sfd.FileName);
                if (Path.GetExtension(sfd.FileName) == ".xaml")
                {
                    File.WriteAllText(sfd.FileName, xaml);
                }
                else
                {
                    SaveBitmapToDisk(image, sfd.FileName);
                    await OptimizeImageAsync(sfd.FileName);
                }
                return true;
            }

            return false;
        }

        private static void EnsureDirectory(string fileName)
        {
            var fileParentPath = Path.GetDirectoryName(fileName);

            if (Directory.Exists(fileParentPath) == false)
            {
                Directory.CreateDirectory(fileParentPath);
            }
        }

        private static void SaveBitmapToDisk(BitmapSource image, string fileName)
        {
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
