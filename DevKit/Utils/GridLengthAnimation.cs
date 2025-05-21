using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace DevKit.Utils
{
    public class GridLengthAnimation : AnimationTimeline
    {
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        public override Type TargetPropertyType => typeof(GridLength);

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue,
            AnimationClock animationClock)
        {
            var fromVal = From.Value;
            var toVal = To.Value;

            var progress = animationClock.CurrentProgress.Value;

            var current = fromVal + (toVal - fromVal) * progress;

            return new GridLength(current,
                From.IsStar ? GridUnitType.Star : To.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
        }
    }
}