using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace InventorySystem.Utils
{
    public static class AnimationExtensions
    {
        public static Task SlideInFromLeft(this VisualElement element, double targetWidth, uint length = 250)
        {
            element.AbortAnimation("SlideIn");
            var tcs = new TaskCompletionSource<bool>();

            // Setup awal
            element.AnchorX = 0; // pivot kiri
            element.InputTransparent = true;
            element.IsVisible = true;
            element.WidthRequest = targetWidth;
            element.TranslationX = -targetWidth;

            // Animasi dengan easing yang halus
            element.Animate(
                name: "SlideIn",
                callback: v => element.TranslationX = v,
                start: -targetWidth,
                end: 0,
                length: length,
                easing: Easing.CubicOut,
                finished: (v, cancelled) =>
                {
                    element.TranslationX = 0;
                    element.InputTransparent = false;
                    tcs.SetResult(!cancelled);
                }
            );

            return tcs.Task;
        }

        public static Task SlideOutToLeft(this VisualElement element, double targetWidth, uint length = 250)
        {
            element.AbortAnimation("SlideOut");
            var tcs = new TaskCompletionSource<bool>();

            element.AnchorX = 0;
            element.InputTransparent = true;
            // pastikan start posisi 0 (visible area)
            element.TranslationX = 0;

            element.Animate(
                name: "SlideOut",
                callback: v => element.TranslationX = v,
                start: 0,
                end: -targetWidth,
                length: length,
                easing: Easing.CubicIn,
                finished: (v, cancelled) =>
                {
                    element.TranslationX = -targetWidth;
                    element.InputTransparent = false;
                    tcs.SetResult(!cancelled);
                }
            );

            return tcs.Task;
        }

        public static Task SlideInFromRight(this VisualElement element, double targetWidth, uint length = 250)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            element.TranslationX = targetWidth;
            element.WidthRequest = targetWidth;
            
            element.Animate("SlideIn", v => {
                element.TranslationX = v;
            }, targetWidth, 0, length: length, finished: (v, cancelled) => {
                tcs.SetResult(!cancelled);
            });
            
            return tcs.Task;
        }

        public static Task SlideOutToRight(this VisualElement element, double targetWidth, uint length = 250)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            element.Animate("SlideOut", v => {
                element.TranslationX = v;
            }, 0, targetWidth, length: length, finished: (v, cancelled) => {
                element.TranslationX = targetWidth;
                tcs.SetResult(!cancelled);
            });
            
            return tcs.Task;
        }

        public static Task WidthTo(this ColumnDefinition column, double newWidth, View animator, uint length = 250)
        {
            double startWidth = column.Width.Value;

            var tcs = new TaskCompletionSource<bool>();

            animator.Animate(
                name: "ColumnWidthAnimation",
                callback: value =>
                {
                    column.Width = new GridLength(value);
                },
                start: startWidth,
                end: newWidth,
                length: length,
                easing: Easing.Linear,
                finished: (v, c) => tcs.SetResult(true)
            );

            return tcs.Task;
        }
    }
}
