using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KnownMonikersExplorer.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KnownMonikersExplorer.Tests
{
    [TestClass]
    public sealed class ImageExportTests
    {
        [STATestMethod]
        [DataRow("image.png", 16, 32, "89504E470D0A1A0A")]
        [DataRow("image.jpg", 32, 16, "FFD8")]
        [DataRow("image.jpeg", 1, 1, "FFD8")]
        [DataRow("image.gif", 16, 16, "47494638")]
        public void SavedImagesDecodeWithCorrectDimensionsAndRepresentativePixels(string fileName, int width, int height, string signature)
        {
            var pixels = new byte[width * height * 4];
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 2] = 255;
                pixels[i + 3] = 255;
            }

            BitmapSource image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            var output = new TrackingStream();
            var opens = 0;

            ImageExport.Save(image, fileName, path =>
            {
                Assert.AreEqual(fileName, path);
                opens++;
                return output;
            });

            Assert.AreEqual(1, opens);
            Assert.IsTrue(output.Disposed);
            Assert.StartsWith(signature, BitConverter.ToString(output.ToArray()).Replace("-", string.Empty));

            using (var input = new MemoryStream(output.ToArray()))
            {
                BitmapDecoder decoder = BitmapDecoder.Create(input, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                Assert.AreEqual(1, decoder.Frames.Count);
                Assert.AreEqual(width, decoder.Frames[0].PixelWidth);
                Assert.AreEqual(height, decoder.Frames[0].PixelHeight);

                var decoded = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
                var actualPixels = new byte[pixels.Length];
                decoded.CopyPixels(actualPixels, width * 4, 0);
                for (var i = 0; i < actualPixels.Length; i += 4)
                {
                    Assert.IsTrue(actualPixels[i] <= 10, "Blue channel must remain near zero.");
                    Assert.IsTrue(actualPixels[i + 1] <= 10, "Green channel must remain near zero.");
                    Assert.IsTrue(actualPixels[i + 2] >= 245, "Red channel must remain near full intensity.");
                    Assert.AreEqual((byte)255, actualPixels[i + 3]);
                }
            }
        }

        [STATestMethod]
        public void PngPreservesTransparencyAndExactPixels()
        {
            byte[] pixels = { 0, 0, 255, 255, 0, 255, 0, 128, 255, 0, 0, 0, 3, 5, 7, 255 };
            BitmapSource image = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
            var output = new TrackingStream();

            ImageExport.Save(image, "image.png", _ => output);

            using (var input = new MemoryStream(output.ToArray()))
            {
                BitmapDecoder decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var decoded = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
                var actual = new byte[pixels.Length];
                decoded.CopyPixels(actual, 8, 0);
                CollectionAssert.AreEqual(pixels, actual);
            }
        }

        [STATestMethod]
        [DataRow("existing.bmp")]
        [DataRow("existing.svg")]
        [DataRow("existing")]
        public void UnsupportedExtensionNeverOpensOrTruncatesExistingDestination(string fileName)
        {
            byte[] existing = { 1, 2, 3, 4 };
            var opened = false;
            BitmapSource image = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);

            Assert.Throws<NotSupportedException>(() => ImageExport.Save(image, fileName, _ =>
            {
                opened = true;
                existing = Array.Empty<byte>();
                return new TrackingStream();
            }));

            Assert.IsFalse(opened);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, existing);
        }

        [STATestMethod]
        public void DestinationFailureIsPropagatedWithoutSuccessFallback()
        {
            var failure = new IOException("Destination unavailable.");
            BitmapSource image = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);

            IOException actual = Assert.Throws<IOException>(() => ImageExport.Save(image, "image.png", _ => throw failure));

            Assert.AreSame(failure, actual);
        }

        [STATestMethod]
        public void EncodingFailureClosesDestinationAndPropagatesError()
        {
            var output = new TrackingStream();

            Assert.Throws<ArgumentNullException>(() => ImageExport.Save(null, "image.png", _ => output));

            Assert.IsTrue(output.Disposed);
            Assert.AreEqual(0, output.ToArray().Length);
        }

        [STATestMethod]
        public void WritingFailureClosesDestinationAndReportsEncodingFailure()
        {
            var output = new FailingStream();
            BitmapSource image = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);

            // WPF's native encoder wraps a stream write failure in InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => ImageExport.Save(image, "image.png", _ => output));

            Assert.IsGreaterThan(0, output.WriteAttempts);
            Assert.IsTrue(output.Disposed);
        }

        private class TrackingStream : MemoryStream
        {
            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class FailingStream : TrackingStream
        {
            public int WriteAttempts { get; private set; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteAttempts++;
                throw new IOException("Write failed.");
            }
        }
    }
}