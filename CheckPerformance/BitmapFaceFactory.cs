namespace CheckPerformance;

using AnimatedImage;

internal class BitmapFaceFactory : IBitmapFaceFactory
{
    public IBitmapFace Create(int width, int height)
        => new BitmapFace(width, height);
}
