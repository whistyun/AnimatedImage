namespace AnimatedImage
{
    /// <summary>
    /// The wrapper for image rendering.
    /// </summary>
    public interface IBitmapFace
    {
        /// <summary>
        /// Writes BGRA pixels to the specified area in the image.
        /// </summary>
        /// <remarks>
        /// Pixels are represented as a byte array. 
        /// For each pixel is allocated 32bit.Pixel has 4 channels; blue, green, red and alpha.
        /// Each channel is allocated 8 bits per pixel.
        /// </remarks>
        /// <param name="buffer">The pixel array</param>
        /// <param name="x">The left position of the specified area</param>
        /// <param name="y">The top position of the specified area</param>
        /// <param name="width">The width of the specified area</param>
        /// <param name="height">The height of the specified area</param>
        public void WriteBGRA(byte[] buffer, int x, int y, int width, int height);

        /// <summary>
        /// Reads BGRA pixels to the specified area in the image.
        /// </summary>
        /// <remarks>
        /// Pixels are represented as a byte array. 
        /// For each pixel is allocated 32bit.Pixel has 4 channels; blue, green, red and alpha.
        /// Each channel is allocated 8 bits per pixel.
        /// </remarks>
        /// <param name="buffer">The pixel array</param>
        /// <param name="x">The left position of the specified area</param>
        /// <param name="y">The top position of the specified area</param>
        /// <param name="width">The width of the specified area</param>
        /// <param name="height">The height of the specified area</param>
        public void ReadBGRA(byte[] buffer, int x, int y, int width, int height);

        /// <summary>
        /// Writes 0 to the specified area in the image.
        /// </summary>
        /// <param name="x">The left position of the specified area</param>
        /// <param name="y">The top position of the specified area</param>
        /// <param name="width">The width of the specified area</param>
        /// <param name="height">The height of the specified area</param>
        public void Clear(int x, int y, int width, int height);
    }
}
