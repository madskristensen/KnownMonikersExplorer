using System;
using System.IO;
using System.Runtime.InteropServices;
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
        private static IVsImageService2 _imageService;
        private static DTE2 _dte;

        public ExportMonikerWindow(KnownMonikersViewModel model, IVsImageService2 imageService, DTE2 dte)
        {
            _model = model;
            _imageService = imageService;
            _dte = dte;
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Icon = GetImage(KnownMonikers.Export, 16);
            imgMoniker.Moniker = _model.Moniker;

            txtSize.Focus();
        }

        public static BitmapSource GetImage(ImageMoniker moniker, int size)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                Dpi = 96,
                LogicalHeight = size,
                LogicalWidth = size,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
            };

            IVsUIObject result = _imageService.GetImage(moniker, imageAttributes);

            result.get_Data(out var data);

            if (data == null)
            {
                return null;
            }

            return data as BitmapSource;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (int.TryParse(txtSize.Text, out var size))
            {
                BitmapSource image = GetImage(_model.Moniker, size);
                var saved = SaveImage(image, _model.Name);

                if (saved)
                {
                    Close();
                }
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
                System.Diagnostics.Debug.Write(ex);
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
