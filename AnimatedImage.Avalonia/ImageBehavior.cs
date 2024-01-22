using Avalonia;
using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Provides attached properties that display animated image in a standard Image control.
    /// </summary>
    public class ImageBehavior
    {
        /// <summary>
        /// Identifies the <c>AnimatedSource</c> attached property.
        /// </summary>
        public static readonly AttachedProperty<IBitmapSource> AnimatedSourceProperty =
            AvaloniaProperty.RegisterAttached<ImageBehavior, Image, IBitmapSource>("AnimatedSource");

        /// <summary>
        /// Identifies the <c>RepeatBehavior</c> attached property.
        /// </summary>
        public static readonly AttachedProperty<double> SpeedRatioProperty =
            AvaloniaProperty.RegisterAttached<ImageBehavior, Image, double>("SpeedRatio", 1d);

        /// <summary>
        /// Identifies the <c>RepeatBehavior</c> attached property.
        /// </summary>
        public static readonly AttachedProperty<RepeatBehavior> RepeatBehaviorProperty =
            AvaloniaProperty.RegisterAttached<ImageBehavior, Image, RepeatBehavior>("RepeatBehavior", RepeatBehavior.Default);


        static ImageBehavior()
        {
            AnimatedSourceProperty.Changed.Subscribe(Observer.Create<IBitmapSource>(HandleAnimatedSourceChanged));
            SpeedRatioProperty.Changed.Subscribe(Observer.Create<double>(HandleSpeedRatioChanged));
            RepeatBehaviorProperty.Changed.Subscribe(Observer.Create<RepeatBehavior>(HandleRepeatBehavior));
        }

        private static void HandleAnimatedSourceChanged(AvaloniaObject element, IBitmapSource? animatedSource)
        {
            if (element is Image image)
            {
                foreach (var oldStyle in image.Styles.OfType<AnimationStyle>().ToArray())
                {
                    oldStyle.Dispose();
                }

                if (animatedSource is not null
                 && FrameRendererCreator.TryCreate(animatedSource, out var renderer))
                {
                    var behavior = GetRepeatBehavior(image);
                    var speedRatio = GetSpeedRatio(image);
                    AnimationStyle.Setup(image, speedRatio, behavior, renderer);
                }
            }
        }
        private static void HandleSpeedRatioChanged(AvaloniaObject element, double speedRatio)
        {
            if (element is Image image)
            {
                foreach (var oldStyle in image.Styles.OfType<AnimationStyle>().ToArray())
                {
                    oldStyle.SetSpeedRatio(speedRatio);
                }
            }
        }
        private static void HandleRepeatBehavior(AvaloniaObject element, RepeatBehavior behavior)
        {
            if (element is Image image)
            {
                foreach (var oldStyle in image.Styles.OfType<AnimationStyle>().ToArray())
                {
                    oldStyle.SetRepeatBehavior(behavior);
                }
            }
        }

        /// <summary>
        /// Sets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="uri">The animated image to display.</param>
        public static void SetAnimatedSource(AvaloniaObject obj, Uri uri)
            => ((Image)obj).SetValue(AnimatedSourceProperty, uri);

        /// <summary>
        /// Sets the value of the <c>SpeedRatio</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="ratio">The speed ratio of the animated image.</param>
        /// <remarks>The <c>AnimationSpeedRatio</c> and <c>AnimationDuration</c> properties are mutually exclusive, only one can be set at a time.</remarks>
        public static void SetSpeedRatio(AvaloniaObject obj, double ratio)
            => ((Image)obj).SetValue(SpeedRatioProperty, ratio);

        /// <summary>
        /// Sets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="behavior">The repeat behavior of the animated image.</param>
        public static void SetRepeatBehavior(AvaloniaObject obj, RepeatBehavior behavior)
            => ((Image)obj).SetValue(RepeatBehaviorProperty, behavior);

        /// <summary>
        /// Gets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The currently displayed animated image.</returns>
        public static IBitmapSource GetAnimatedSource(AvaloniaObject obj)
            => obj.GetValue(AnimatedSourceProperty);

        /// <summary>
        /// Gets the value of the <c>SpeedRatio</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The speed ratio for the animated image.</returns>
        public static double GetSpeedRatio(AvaloniaObject obj)
            => obj.GetValue(SpeedRatioProperty);

        /// <summary>
        /// Gets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The repeat behavior of the animated image.</returns>
        public static RepeatBehavior GetRepeatBehavior(AvaloniaObject obj)
            => obj.GetValue(RepeatBehaviorProperty);
    }
}
