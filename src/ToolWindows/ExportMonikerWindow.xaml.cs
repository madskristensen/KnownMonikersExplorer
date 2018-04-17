using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
            int.TryParse(txtSize.Text, out int size);

            BitmapSource image = GetImage(_model.Moniker, size);
            bool saved = SaveImage(image, _model.Name);
        }

        private bool SaveImage(BitmapSource image, string name)
        {
            var sfd = new SaveFileDialog
            {
                FileName = name + ".png",
                DefaultExt = ".png"
            };

            DialogResult saved = sfd.ShowDialog();

            if (saved == System.Windows.Forms.DialogResult.OK)
            {
                SaveBitmapToDisk(image, sfd.FileName);
            }

            return saved == System.Windows.Forms.DialogResult.OK;
        }

        private static void SaveBitmapToDisk(BitmapSource image, string fileName)
        {
            string fileParentPath = Path.GetDirectoryName(fileName);

            if (Directory.Exists(fileParentPath) == false)
                Directory.CreateDirectory(fileParentPath);

            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
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
