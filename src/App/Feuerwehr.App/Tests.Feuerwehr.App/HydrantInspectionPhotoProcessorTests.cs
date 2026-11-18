namespace de.openelp.feuerwehr.Tests.App
{
    using System.Runtime.InteropServices;
    using de.openelp.feuerwehr.App.Models;
    using de.openelp.feuerwehr.App.Services;
    using de.openelp.feuerwehr.Common.Models.HydrantInspections;
    using SkiaSharp;

    public sealed class HydrantInspectionPhotoProcessorTests
    {
        [Theory]
        [InlineData(3072, 4080, 1542, 2048)]
        [InlineData(4080, 3072, 2048, 1542)]
        [InlineData(4096, 4096, 2048, 2048)]
        [InlineData(640, 480, 640, 480)]
        [InlineData(2048, 1000, 2048, 1000)]
        [InlineData(1, 4080, 1, 2048)]
        public async Task ProducesJpegWithinTheConfirmedDimensionsWithoutUpscaling(int width, int height, int expectedWidth, int expectedHeight)
        {
            using SKBitmap source = new SKBitmap(width, height);
            source.Erase(SKColors.CornflowerBlue);
            using MemoryStream input = Encode(source, SKEncodedImageFormat.Png);
            PreparedHydrantInspectionPhoto result = await new HydrantInspectionPhotoProcessor().PrepareAsync(input);
            using SKCodec codec = SKCodec.Create(new MemoryStream(result.Content));
            Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
            Assert.Equal(expectedWidth, result.Width);
            Assert.Equal(expectedHeight, result.Height);
            Assert.Equal(expectedWidth, codec.Info.Width);
            Assert.Equal(expectedHeight, codec.Info.Height);
            Assert.Equal("image/jpeg", result.ContentType);
            Assert.InRange(result.Content.Length, 1, 5 * 1024 * 1024);
            Assert.True(input.CanRead);
        }

        [Theory]
        [InlineData(1, 0, 1, 2, 3)]
        [InlineData(2, 1, 0, 3, 2)]
        [InlineData(3, 3, 2, 1, 0)]
        [InlineData(4, 2, 3, 0, 1)]
        [InlineData(5, 0, 2, 1, 3)]
        [InlineData(6, 2, 0, 3, 1)]
        [InlineData(7, 3, 1, 2, 0)]
        [InlineData(8, 1, 3, 0, 2)]
        public async Task AppliesAllExifRotationsAndMirroringBeforeDiscardingTheMetadata(int orientation, int topLeft, int topRight, int bottomLeft, int bottomRight)
        {
            SKColor[] colors = [SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow];
            using SKBitmap source = new SKBitmap(160, 100);
            using (SKCanvas canvas = new SKCanvas(source))
            {
                using SKPaint paint = new SKPaint();
                for (int index = 0; index < colors.Length; index++)
                {
                    paint.Color = colors[index];
                    canvas.DrawRect(new SKRect((index % 2) * 80, (index / 2) * 50, ((index % 2) + 1) * 80, ((index / 2) + 1) * 50), paint);
                }
            }

            using MemoryStream jpeg = Encode(source, SKEncodedImageFormat.Jpeg);
            using MemoryStream input = AddOrientation(jpeg.ToArray(), orientation);
            using (SKCodec originalCodec = SKCodec.Create(new MemoryStream(input.ToArray())))
            {
                Assert.Equal((SKEncodedOrigin)orientation, originalCodec.EncodedOrigin);
            }

            PreparedHydrantInspectionPhoto result = await new HydrantInspectionPhotoProcessor().PrepareAsync(input);
            using SKBitmap output = SKBitmap.Decode(result.Content);
            Assert.Equal(orientation >= 5 ? 100 : 160, output.Width);
            Assert.Equal(orientation >= 5 ? 160 : 100, output.Height);
            AssertColor(colors[topLeft], output.GetPixel(output.Width / 4, output.Height / 4));
            AssertColor(colors[topRight], output.GetPixel(3 * output.Width / 4, output.Height / 4));
            AssertColor(colors[bottomLeft], output.GetPixel(output.Width / 4, 3 * output.Height / 4));
            AssertColor(colors[bottomRight], output.GetPixel(3 * output.Width / 4, 3 * output.Height / 4));
            using SKCodec codec = SKCodec.Create(new MemoryStream(result.Content));
            Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
        }

        [Fact]
        public async Task DownsizesLargeJpegAndRejectsAnIncompleteJpeg()
        {
            using SKBitmap source = new SKBitmap(4080, 3072);
            source.Erase(SKColors.Red);
            using MemoryStream input = Encode(source, SKEncodedImageFormat.Jpeg);
            byte[] bytes = input.ToArray();
            PreparedHydrantInspectionPhoto result = await new HydrantInspectionPhotoProcessor().PrepareAsync(input);
            Assert.Equal(2048, result.Width);
            Assert.Equal(1542, result.Height);
            using MemoryStream incomplete = new MemoryStream(bytes, 0, bytes.Length / 2);
            await Assert.ThrowsAsync<InvalidDataException>(() => new HydrantInspectionPhotoProcessor().PrepareAsync(incomplete));
        }

        [Fact]
        public async Task AppliesTheUploadLimitToTheConvertedJpegNotTheOriginalImage()
        {
            using SKBitmap source = new SKBitmap(new SKImageInfo(2560, 2048, SKColorType.Rgba8888, SKAlphaType.Opaque));
            byte[] pixels = new byte[source.ByteCount];
            new Random(42).NextBytes(pixels);
            for (int index = 3; index < pixels.Length; index += 4)
            {
                pixels[index] = 255;
            }

            Marshal.Copy(pixels, 0, source.GetPixels(), pixels.Length);
            using MemoryStream input = Encode(source, SKEncodedImageFormat.Png);
            Assert.True(input.Length > HydrantInspectionPhotoLimits.MaximumBytes);
            PreparedHydrantInspectionPhoto result = await new HydrantInspectionPhotoProcessor().PrepareAsync(input);
            Assert.InRange(result.Content.Length, 1, 5 * 1024 * 1024);
            Assert.Equal(2048, result.Width);
            Assert.Equal(1638, result.Height);
        }

        [Fact]
        public async Task CompositesTransparentPixelsOnWhiteForJpeg()
        {
            using SKBitmap source = new SKBitmap(32, 32);
            source.Erase(SKColors.Transparent);
            using MemoryStream input = Encode(source, SKEncodedImageFormat.Png);
            PreparedHydrantInspectionPhoto result = await new HydrantInspectionPhotoProcessor().PrepareAsync(input);
            using SKBitmap output = SKBitmap.Decode(result.Content);
            AssertColor(SKColors.White, output.GetPixel(16, 16));
        }

        [Fact]
        public async Task AcceptsNonSeekableCameraStreamsWithoutClosingThem()
        {
            using SKBitmap source = new SKBitmap(100, 50);
            source.Erase(SKColors.Red);
            using MemoryStream encoded = Encode(source, SKEncodedImageFormat.Jpeg);
            using NonSeekablePhotoStream input = new NonSeekablePhotoStream(encoded.ToArray());
            PreparedHydrantInspectionPhoto result = await new HydrantInspectionPhotoProcessor().PrepareAsync(input);
            Assert.Equal(100, result.Width);
            Assert.True(input.CanRead);
        }

        [Fact]
        public async Task RejectsUnreadableEmptyAndCorruptedImagesAndHonorsCancellation()
        {
            HydrantInspectionPhotoProcessor processor = new HydrantInspectionPhotoProcessor();
            await Assert.ThrowsAsync<ArgumentNullException>(() => processor.PrepareAsync(null!));
            using MemoryStream empty = new MemoryStream();
            await Assert.ThrowsAsync<InvalidDataException>(() => processor.PrepareAsync(empty));
            using MemoryStream garbage = new MemoryStream([1, 2, 3, 4, 5]);
            await Assert.ThrowsAsync<InvalidDataException>(() => processor.PrepareAsync(garbage));
            using MemoryStream closed = new MemoryStream();
            closed.Close();
            await Assert.ThrowsAsync<ArgumentException>(() => processor.PrepareAsync(closed));
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            using MemoryStream input = new MemoryStream([1, 2, 3]);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.PrepareAsync(input, cancellation.Token));
            Assert.Equal(0, input.Position);
        }

        private static MemoryStream Encode(SKBitmap bitmap, SKEncodedImageFormat format)
        {
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(format, 100);
            return new MemoryStream(data.ToArray());
        }

        private static MemoryStream AddOrientation(byte[] jpeg, int orientation)
        {
            byte[] exif = [0xff, 0xe1, 0, 34, 0x45, 0x78, 0x69, 0x66, 0, 0, 0x49, 0x49, 0x2a, 0, 8, 0, 0, 0,
                1, 0, 0x12, 1, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
            MemoryStream result = new MemoryStream();
            result.Write(jpeg, 0, 2);
            result.Write(exif);
            result.Write(jpeg, 2, jpeg.Length - 2);
            result.Position = 0;
            return result;
        }

        private static void AssertColor(SKColor expected, SKColor actual)
        {
            Assert.InRange(Math.Abs(expected.Red - actual.Red), 0, 20);
            Assert.InRange(Math.Abs(expected.Green - actual.Green), 0, 20);
            Assert.InRange(Math.Abs(expected.Blue - actual.Blue), 0, 20);
        }
    }
}
