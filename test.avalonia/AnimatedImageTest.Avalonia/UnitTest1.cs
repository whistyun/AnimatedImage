using AnimatedImageTest.Avalonia.Xamls;
using ApprovalTests.Reporters;
using ApprovalTests;
using Avalonia;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using UnitTest.Base.Utils;
using UnitTest.Base;
using System;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Threading;
using System.Threading.Tasks;
using AnimatedImage.Avalonia;
using Avalonia.Platform;
using System.Security.Policy;
using Tmds.DBus.Protocol;

namespace AnimatedImageTest.Avalonia
{
    public class UnitTest1 : UnitTestBase
    {
        private UserControl? _control;

        public override Control ContentOfWindow => _control ??= new UserControl1();

        [Test]
        [RunOnUI]
        [Order(0)]
        public void Test1()
        {
            var ctrl = ContentOfWindow;

            var width = ctrl.Bounds.Width;
            var height = ctrl.Bounds.Height;
            var renderSize = new PixelSize((int)width, (int)height);
            var bitmap = new RenderTargetBitmap(renderSize, new Vector(96, 96));
            bitmap.Render(ctrl);
        }

        [Test]
        [RunOnUI]
        [Order(1)]
        public void Test2()
        {
            var ctrl = ContentOfWindow;

            var img1 = ctrl.FindControl<Image>("img1");

            ImageBehavior.SetAnimatedSource(img1, new Uri("avares://AnimatedImageTest.Avalonia/Assets/monster.gif"));
        }

        [Test]
        [RunOnUI]
        [Order(2)]
        public void Test3()
        {
            var ctrl = ContentOfWindow;

            var img2 = ctrl.FindControl<Image>("img2");
            var source = new AnimatedImageSourceStream(AssetLoader.Open(new Uri("avares://AnimatedImageTest.Avalonia/Assets/radar.gif")));

            ImageBehavior.SetAnimatedSource(img2, source);
        }
    }
}