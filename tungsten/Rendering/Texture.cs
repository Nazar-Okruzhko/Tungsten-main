using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using Tungsten.Core;

namespace Tungsten.Rendering
{
    /// <summary>
    /// Loads any image the Studio needs onto the GPU: toolbar icons (.png),
    /// asset-store thumbnails (.jpg/.png), and the boot splash banner.
    ///
    /// NOTE on "Bitmap1076.bmp": the file is, byte-for-byte, a JPEG saved
    /// under a ".bmp" name. StbImageSharp sniffs file *contents*, not the
    /// extension, so it decodes it identically to a real .bmp/.png/.jpg -
    /// no special-casing needed.
    /// </summary>
    public class Texture
    {
        public int Handle { get; }
        public int Width { get; }
        public int Height { get; }

        public Texture(string path)
        {
            // NOTE: no vertical flip here. Flipping on load is the usual
            // convention for 3D surface textures (OpenGL's UV origin is
            // bottom-left), but every texture this class currently loads -
            // toolbar icons, asset-store thumbnails, the splash banner - is
            // 2D UI, and ImmediateUi's DrawQuad already maps v=0 to the TOP
            // of the quad assuming top-row-first image data. Flipping here
            // fought that mapping and rendered every icon upside down. If a
            // textured 3D mesh is added later, flip its UVs (or add a
            // separate flip: bool parameter) rather than flipping globally.
            using var stream = File.OpenRead(path);
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            Width = image.Width;
            Height = image.Height;

            Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            Logger.Info($"Loaded texture '{Path.GetFileName(path)}' ({Width}x{Height})");
        }

        public void Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }
    }
}
