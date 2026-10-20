using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KnownMonikersExplorer.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KnownMonikersExplorer.Tests
{
    [TestClass]
    public sealed class ExportValidationTests
    {
        [TestMethod]
        [DataRow("1", 1)]
        [DataRow("16", 16)]
        [DataRow("32", 32)]
        [DataRow("4095", 4095)]
        [DataRow("4096", 4096)]
        public void ValidSizesAreParsedWithoutChangingTheirValue(string text, int expected)
        {
            Assert.IsTrue(ImageExport.TryGetSize(text, out var size));
            Assert.AreEqual(expected, size);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("0")]
        [DataRow("-1")]
        [DataRow("4097")]
        [DataRow("2147483647")]
        [DataRow("2147483648")]
        [DataRow("16px")]
        [DataRow("16.5")]
        [DataRow("+16")]
        [DataRow(" 16 ")]
        [DataRow(" \t ")]
        [DataRow("1,024")]
        public void InvalidTypedOrPastedSizesAreRejected(string text)
        {
            Assert.IsFalse(ImageExport.TryGetSize(text, out _));
        }

        [STATestMethod]
        [DataRow("image.png", typeof(PngBitmapEncoder), "89504E470D0A1A0A")]
        [DataRow("image.PNG", typeof(PngBitmapEncoder), "89504E470D0A1A0A")]
        [DataRow("image.jpg", typeof(JpegBitmapEncoder), "FFD8")]
        [DataRow("image.JPEG", typeof(JpegBitmapEncoder), "FFD8")]
        [DataRow("image.gif", typeof(GifBitmapEncoder), "47494638")]
        public void SupportedExtensionsProduceMatchingFileFormats(string fileName, Type encoderType, string signature)
        {
            BitmapEncoder encoder = ImageExport.CreateEncoder(fileName);
            Assert.AreEqual(encoderType, encoder.GetType());

            BitmapSource bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 255, 255 }, 4);
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                string header = BitConverter.ToString(stream.ToArray()).Replace("-", string.Empty);
                Assert.StartsWith(signature, header);
            }
        }

        [STATestMethod]
        [DataRow("image.bmp")]
        [DataRow("image.svg")]
        [DataRow("image.txt")]
        [DataRow("image")]
        public void UnsupportedExtensionsAreRejectedRatherThanEncodedAsGif(string fileName)
        {
            Assert.Throws<NotSupportedException>(() => ImageExport.CreateEncoder(fileName));
        }
    }
}