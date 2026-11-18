namespace de.openelp.feuerwehr.App.Services
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using de.openelp.feuerwehr.App.Models;
    using de.openelp.feuerwehr.Common.Models.HydrantInspections;
    using SkiaSharp;

    /// <summary>Applies camera orientation, downsizes and encodes inspection photos as JPEG.</summary>
    public sealed class HydrantInspectionPhotoProcessor : IHydrantInspectionPhotoProcessor
    {
        public async Task<PreparedHydrantInspectionPhoto> PrepareAsync(Stream source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            cancellationToken.ThrowIfCancellationRequested();
            if (!source.CanRead)
            {
                throw new ArgumentException("Der Fotostream ist nicht lesbar.", nameof(source));
            }

            using MemoryStream input = new MemoryStream();
            await source.CopyToAsync(input, cancellationToken).ConfigureAwait(false);
            input.Position = 0;
            return await Task.Run(() => Prepare(input, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private static PreparedHydrantInspectionPhoto Prepare(Stream input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using SKManagedStream managedStream = new SKManagedStream(input, false);
            using SKCodec codec = SKCodec.Create(managedStream) ?? throw new InvalidDataException("Das Fotoformat kann nicht gelesen werden.");
            SKImageInfo original = codec.Info;
            if (original.Width <= 0 || original.Height <= 0)
            {
                throw new InvalidDataException("Das Foto besitzt keine gültigen Bildabmessungen.");
            }

            double scale = Math.Min(1d, (double)HydrantInspectionPhotoLimits.MaximumDimension / Math.Max(original.Width, original.Height));
            int width = Math.Max(1, (int)Math.Round(original.Width * scale, MidpointRounding.AwayFromZero));
            int height = Math.Max(1, (int)Math.Round(original.Height * scale, MidpointRounding.AwayFromZero));
            bool rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            int outputWidth = rotated ? height : width;
            int outputHeight = rotated ? width : height;

            // JPEG codecs can decode at a reduced size, avoiding a full-resolution camera bitmap.
            float decodeScale = (float)scale;
            SKSizeI decodeSize = codec.GetScaledDimensions(decodeScale);
            while ((decodeSize.Width < width || decodeSize.Height < height) && decodeScale < 1f)
            {
                decodeScale = Math.Min(1f, decodeScale + 0.125f);
                decodeSize = codec.GetScaledDimensions(decodeScale);
            }

            using SKColorSpace colorSpace = SKColorSpace.CreateSrgb();
            SKImageInfo decodeInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul, colorSpace);
            using SKBitmap decoded = new SKBitmap();
            if (!decoded.TryAllocPixels(decodeInfo)
                || codec.GetPixels(decodeInfo, decoded.GetPixels()) != SKCodecResult.Success)
            {
                throw new InvalidDataException("Das Foto ist beschädigt oder kann nicht vollständig gelesen werden.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            using SKBitmap output = new SKBitmap(new SKImageInfo(outputWidth, outputHeight, SKColorType.Bgra8888, SKAlphaType.Opaque, colorSpace));
            using (SKCanvas canvas = new SKCanvas(output))
            {
                canvas.Clear(SKColors.White);
                int orientedWidth = rotated ? decoded.Height : decoded.Width;
                int orientedHeight = rotated ? decoded.Width : decoded.Height;
                canvas.Scale((float)outputWidth / orientedWidth, (float)outputHeight / orientedHeight);
                ApplyOrientation(canvas, codec.EncodedOrigin, decoded.Width, decoded.Height);
                using SKImage decodedImage = SKImage.FromBitmap(decoded);
                canvas.DrawImage(decodedImage, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));
            }

            cancellationToken.ThrowIfCancellationRequested();
            using SKImage image = SKImage.FromBitmap(output);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Jpeg, HydrantInspectionPhotoLimits.JpegQuality)
                ?? throw new InvalidDataException("Das Foto konnte nicht als JPEG gespeichert werden.");
            if (encoded.Size > HydrantInspectionPhotoLimits.MaximumBytes)
            {
                throw new InvalidDataException("Das vorbereitete Foto überschreitet die maximale Uploadgröße von 5 MiB.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new PreparedHydrantInspectionPhoto(encoded.ToArray(), outputWidth, outputHeight);
        }

        private static void ApplyOrientation(SKCanvas canvas, SKEncodedOrigin origin, int width, int height)
        {
            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    canvas.Translate(width, 0);
                    canvas.Scale(-1, 1);
                    break;
                case SKEncodedOrigin.BottomRight:
                    canvas.Translate(width, height);
                    canvas.RotateDegrees(180);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Translate(0, height);
                    canvas.Scale(1, -1);
                    break;
                case SKEncodedOrigin.LeftTop:
                    canvas.RotateDegrees(90);
                    canvas.Scale(1, -1);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(height, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightBottom:
                    canvas.Translate(height, width);
                    canvas.RotateDegrees(90);
                    canvas.Scale(-1, 1);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, width);
                    canvas.RotateDegrees(270);
                    break;
            }
        }
    }
}
