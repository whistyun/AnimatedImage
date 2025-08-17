using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal class WebpRendererFrame : FrameRenderFrame
    {
        public WebpRendererFrame(int x, int y, int width, int height, TimeSpan begin, TimeSpan end) : base(x, y, width, height, begin, end)
        {
        }
    }
}
