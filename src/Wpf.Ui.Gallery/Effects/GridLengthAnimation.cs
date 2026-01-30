using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Wpf.Ui.Gallery.Effects;

public class GridLengthAnimation : AnimationTimeline
{
    public override Type TargetPropertyType => typeof(GridLength);

    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(
            nameof(From),
            typeof(GridLength),
            typeof(GridLengthAnimation)
        );

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(
            nameof(To),
            typeof(GridLength),
            typeof(GridLengthAnimation)
        );

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public static readonly DependencyProperty EasingFunctionProperty =
        DependencyProperty.Register(
            nameof(EasingFunction),
            typeof(IEasingFunction),
            typeof(GridLengthAnimation)
        );

    public IEasingFunction? EasingFunction
    {
        get => (IEasingFunction?)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    public override object GetCurrentValue(
        object defaultOriginValue,
        object defaultDestinationValue,
        AnimationClock animationClock
    )
    {
        double fromValue = From.IsAuto
            ? ((GridLength)defaultOriginValue).Value
            : From.Value;
        double toValue = To.IsAuto
            ? ((GridLength)defaultDestinationValue).Value
            : To.Value;

        double progress = animationClock.CurrentProgress ?? 0d;

        IEasingFunction? easing = EasingFunction;
        if (easing != null)
        {
            progress = easing.Ease(progress);
        }

        double value = fromValue + ((toValue - fromValue) * progress);

        return new GridLength(value, GridUnitType.Pixel);
    }

    protected override Freezable CreateInstanceCore()
    {
        return new GridLengthAnimation();
    }
}

