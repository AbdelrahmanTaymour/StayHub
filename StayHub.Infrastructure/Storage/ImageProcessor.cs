using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StayHub.Application.Abstractions.Storage;

namespace StayHub.Infrastructure.Storage;

/// <summary>
///     Processes user-uploaded images before they are persisted to object storage.
/// </summary>
/// <remarks>
///     <para>
///         Apartment photos are resized to a web-friendly maximum dimension and watermarked.
///         User avatars are center-cropped to a square and resized to a fixed maximum size.
///     </para>
///     <para>
///         All output is encoded as JPEG and EXIF metadata is removed before storage.
///         This keeps the stored representation predictable and prevents accidental exposure
///         of metadata such as GPS coordinates and device information.
///     </para>
///     <para>
///         The watermark font is loaded from the application's bundled Fonts directory rather
///         than relying on fonts installed on the host operating system. This is important for
///         containerized deployments where the base ASP.NET runtime image may not contain
///         application fonts.
///     </para>
/// </remarks>
internal sealed class ImageProcessor : IImageProcessor
{
    // -------------------------------------------------------------------------
    // Apartment photos
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Maximum width or height of an apartment photo after processing.
    ///     Images smaller than this are never upscaled.
    /// </summary>
    private const int ApartmentPhotoMaxDimension = 1920;

    /// <summary>
    ///     JPEG quality used for apartment photos.
    /// </summary>
    private const int ApartmentPhotoJpegQuality = 82;

    // -------------------------------------------------------------------------
    // User avatars
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Maximum width and height of processed avatars.
    ///     Avatars are center-cropped to a square before resizing.
    /// </summary>
    private const int AvatarTargetSize = 512;

    /// <summary>
    ///     JPEG quality used for user avatars.
    /// </summary>
    private const int AvatarJpegQuality = 85;

    // -------------------------------------------------------------------------
    // Watermark
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Watermark font size as a proportion of the processed image width.
    /// </summary>
    private const float WatermarkFontSizeRatio = 0.035f;

    /// <summary>
    ///     Minimum watermark font size in pixels.
    ///     Prevents the watermark from becoming unreadably small on small images.
    /// </summary>
    private const float WatermarkMinFontSize = 14f;

    /// <summary>
    ///     Distance between the watermark and the image edges as a proportion
    ///     of the processed image width.
    /// </summary>
    private const float WatermarkPaddingRatio = 0.02f;

    /// <summary>
    ///     Text rendered over apartment photos.
    /// </summary>
    private const string WatermarkText = "StayHub";

    /// <summary>
    ///     Font family used by the watermark.
    ///     Loaded once per application process instead of once per image.
    /// </summary>
    private static readonly FontFamily WatermarkFontFamily = LoadWatermarkFontFamily();

    /// <inheritdoc />
    public async Task<ProcessedImage> ProcessAsync(
        Stream original,
        ImageCategory category,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);

        using var image = await Image.LoadAsync<Rgba32>(original, cancellationToken);

        // Some cameras store the physical rotation in EXIF metadata instead of
        // rotating the actual pixels. Apply the orientation before removing EXIF
        // metadata so the resulting image remains visually correct.
        image.Mutate(x => x.AutoOrient());

        switch (category)
        {
            case ImageCategory.ApartmentPhoto:
                ResizeDownOnly(image, ApartmentPhotoMaxDimension);
                ApplyWatermark(image);
                break;

            case ImageCategory.UserAvatar:
                CropToSquareAndResize(image, AvatarTargetSize);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(category), category, "Unhandled image category.");
        }

        // Remove EXIF metadata before the image leaves the application.
        //
        // EXIF can contain information such as GPS coordinates, camera/device
        // information, timestamps, and the original orientation. Orientation has
        // already been applied above, so retaining the EXIF profile is unnecessary.
        image.Metadata.ExifProfile = null;

        var quality = category switch
        {
            ImageCategory.ApartmentPhoto => ApartmentPhotoJpegQuality,
            ImageCategory.UserAvatar => AvatarJpegQuality,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };

        // Always produce JPEG output so object storage and consumers have a
        // predictable content type and file extension.
        //
        // JPEG does not support transparency. This is intentional because both
        // supported categories represent photographs rather than transparent
        // graphics.
        var output = new MemoryStream();

        var encoder = new JpegEncoder
        {
            Quality = quality
        };

        await image.SaveAsJpegAsync(output, encoder, cancellationToken);

        output.Position = 0;

        return new ProcessedImage(output, "image/jpeg", ".jpg");
    }

    /// <summary>
    ///     Reduces an image so that neither dimension exceeds <paramref name="maxDimension" />.
    /// </summary>
    /// <remarks>
    ///     The original aspect ratio is preserved and images are never upscaled.
    /// </remarks>
    private static void ResizeDownOnly(Image<Rgba32> image, int maxDimension)
    {
        if (image.Width <= maxDimension && image.Height <= maxDimension)
            return;

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxDimension, maxDimension),
            Mode = ResizeMode.Max
        }));
    }

    /// <summary>
    ///     Center-crops an image to a square and then downsizes it if necessary.
    /// </summary>
    /// <remarks>
    ///     Images smaller than <paramref name="targetSize" /> are not upscaled.
    /// </remarks>
    private static void CropToSquareAndResize(Image<Rgba32> image, int targetSize)
    {
        var squareSide = Math.Min(image.Width, image.Height);

        var cropX = (image.Width - squareSide) / 2;
        var cropY = (image.Height - squareSide) / 2;

        image.Mutate(x => x.Crop(
            new Rectangle(cropX, cropY, squareSide, squareSide)));

        if (squareSide <= targetSize) return;

        image.Mutate(x => x.Resize(targetSize, targetSize));
    }

    /// <summary>
    ///     Applies the StayHub watermark to an apartment photo.
    /// </summary>
    /// <remarks>
    ///     The watermark size and padding scale with the processed image width,
    ///     keeping its visual appearance consistent across different resolutions.
    ///     A subtle shadow is rendered before the white text to maintain readability
    ///     against both light and dark photographs.
    /// </remarks>
    private static void ApplyWatermark(Image<Rgba32> image)
    {
        var fontSize = Math.Max(WatermarkMinFontSize, image.Width * WatermarkFontSizeRatio);

        var font = WatermarkFontFamily.CreateFont(fontSize, FontStyle.Regular);

        var padding = image.Width * WatermarkPaddingRatio;

        var textTopLeft = new PointF(padding, image.Height - padding - fontSize);

        var shadowOffset = Math.Max(1f, fontSize * 0.04f);

        var shadowLocation = new PointF(textTopLeft.X + shadowOffset, textTopLeft.Y + shadowOffset);

        var shadowOptions = new RichTextOptions(font)
        {
            Origin = shadowLocation
        };

        var textOptions = new RichTextOptions(font)
        {
            Origin = textTopLeft
        };

        image.Mutate(ctx =>
        {
            ctx.Paint(canvas =>
            {
                // The explicit `pen: null` is important because the DrawText
                // overload accepts both a brush and an optional pen.
                canvas.DrawText(
                    shadowOptions,
                    WatermarkText.AsSpan(),
                    Brushes.Solid(Color.Black.WithAlpha(0.35f)),
                    null);

                canvas.DrawText(
                    textOptions,
                    WatermarkText.AsSpan(),
                    Brushes.Solid(Color.White.WithAlpha(0.75f)),
                    null);
            });
        });
    }

    /// <summary>
    ///     Loads the bundled watermark font from the application's output directory.
    /// </summary>
    /// <remarks>
    ///     The font is deliberately loaded from an application-owned file instead of
    ///     the operating system's installed fonts. This makes the image-processing
    ///     behavior deterministic across local development, CI, and Docker deployments.
    ///     The corresponding font file must be copied to:
    ///     <c>Fonts/Inter-Variable.ttf</c>
    ///     in the application's output directory.
    /// </remarks>
    private static FontFamily LoadWatermarkFontFamily()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Variable.ttf");

        if (!File.Exists(fontPath))
            throw new FileNotFoundException(
                "The StayHub watermark font was not found. " +
                "Ensure 'Inter-Variable.ttf' exists under " +
                "'Fonts/' and is configured to be copied to the application output directory.",
                fontPath);

        var collection = new FontCollection();

        return collection.Add(fontPath);
    }
}