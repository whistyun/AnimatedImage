using System;

namespace AnimatedImage
{
    /// <summary>
    /// Encapsulates frame area and drawing time.
    /// </summary>
    public class FrameRenderFrame
    {
        /// <summary>
        /// The left positions of the frame area.
        /// </summary>
        public int X { get; }
        /// <summary>
        /// The top positions of the frame area.
        /// </summary>
        public int Y { get; }
        /// <summary>
        /// The width of the frame area.
        /// </summary>
        public int Width { get; }
        /// <summary>
        /// The height of the frame area.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The elapsed time until the specified frame is drawn
        /// </summary>
        public TimeSpan Begin { get; }
        /// <summary>
        /// The elapsed time until the drawing result of the specified frame is disposes
        /// </summary>
        public TimeSpan End { get; }

        /// <summary>
        /// Creates the insntace.
        /// </summary>
        /// <param name="x"><see cref="X"/></param>
        /// <param name="y"><see cref="Y"/></param>
        /// <param name="width"><see cref="Width"/></param>
        /// <param name="height"><see cref="Height"/></param>
        /// <param name="begin"><see cref="Begin"/></param>
        /// <param name="end"><see cref="End"/></param>
        public FrameRenderFrame(int x, int y, int width, int height, TimeSpan begin, TimeSpan end)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Begin = begin;
            End = end;
        }

        /// <summary>
        /// Determines whether this instance contains the specified area.
        /// </summary>
        /// <param name="frame">The specified area.</param>
        /// <returns>Returns true if this instance contains the specified area.</returns>
        public bool IsInvolve(FrameRenderFrame frame)
        {
            return X <= frame.X
                && Y <= frame.Y
                && frame.X + frame.Width <= X + Width
                && frame.Y + frame.Height <= Y + Height;
        }
    }
}
