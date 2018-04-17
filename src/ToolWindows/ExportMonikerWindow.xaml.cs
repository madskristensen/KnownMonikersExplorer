using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        public ExportMonikerWindow(KnownMonikersViewModel model, IVsImageService2 imageService)
        {
            _model = model;
            _imageService = imageService;

            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Icon = GetImage(KnownMonikers.Export, 16);
            imgMoniker.Moniker = _model.Moniker;

            txtSize.Focus();
            txtSize.SelectAll();
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

            result.get_Data(out object data);

            if (data == null)
                return null;

            return data as BitmapSource;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSize.Text, out int size))
            {
                BitmapSource image = GetImage(_model.Moniker, size);
                bool saved = SaveImage(image, _model.Name);

                if (saved)
                {
                    Close();
                }
            }
        }

        private bool SaveImage(BitmapSource image, string name)
        {
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
                return true;
            }

            return false;
        }

        private static void SaveBitmapToDisk(BitmapSource image, string fileName)
        {
            string fileParentPath = Path.GetDirectoryName(fileName);

            if (Directory.Exists(fileParentPath) == false)
                Directory.CreateDirectory(fileParentPath);

            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                BitmapEncoder encoder = GetEncoder(fileName);
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }

        private static BitmapEncoder GetEncoder(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

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

            public ImageSource Image
            {
                get
                {
                    return GetImage(Moniker, 16);
                }
            }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}
