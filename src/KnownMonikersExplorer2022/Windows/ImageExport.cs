using System;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;

namespace KnownMonikersExplorer.Windows
{
    internal static class ImageExport
    {
        internal const int MaximumSize = 4096;

        internal static bool TryGetSize(string text, out int size)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out size)
                && size >= 1 && size <= MaximumSize;
        }

        internal static BitmapEncoder CreateEncoder(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLowerInvariant())
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

        internal static void Save(BitmapSource image, string fileName, Func<string, Stream> openFile)
        {
            BitmapEncoder encoder = CreateEncoder(fileName);
            using (Stream stream = openFile(fileName))
            {
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }
    }
}