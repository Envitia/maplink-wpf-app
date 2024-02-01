using System.Collections.Generic;
using System.Windows;

namespace MapLinkProApp
{
  public class SliderProperty : DependencyObject
  {
    public static readonly DependencyProperty ToolTipProperty =
        DependencyProperty.Register(
            name: "ToolTips",
            propertyType: typeof(List<double>),
            ownerType: typeof(SliderProperty),
            typeMetadata: new FrameworkPropertyMetadata());

    public List<double> ToolTips
    {
      get => (List<double>)GetValue(ToolTipProperty);
      set => SetValue(ToolTipProperty, value);
    }
  }
}
