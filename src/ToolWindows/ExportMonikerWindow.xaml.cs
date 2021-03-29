using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using EnvDTE80;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace KnownMonikersExplorer.ToolWindows
{
    public partial class ExportMonikerWindow : Window
    {
        private static KnownMonikersViewModel _model;
        private static DTE2 _dte;

        public ExportMonikerWindow(KnownMonikersViewModel model, DTE2 dte)
        {
            _model = model;
            _dte = dte;
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
                    var saved = SaveImage(image, _model.Name);

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

        private bool SaveImage(BitmapSource image, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                OptimizeImage(sfd.FileName);
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

        private static void OptimizeImage(string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                EnvDTE.Command cmd = _dte.Commands.Item("ImageOptimizer.OptimizeLossless");

                if (cmd != null && cmd.IsAvailable)
                {
                    _dte.Commands.Raise(cmd.Guid, cmd.ID, fileName, null);
                }
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
