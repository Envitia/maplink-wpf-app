using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MapLinkProApp
{
  /// <summary>
  /// Special class to show the right tooltip for the Depth Slider
  /// </summary>
  public class SliderWithCustomToolTip : Slider
  {
    private ToolTip _autoToolTip;
    private List<double> tooltipValues = new List<double>();

    protected override void OnThumbDragStarted(DragStartedEventArgs e)
    {
      base.OnThumbDragStarted(e);
      this.SetToolTipContent();
    }

    protected override void OnThumbDragDelta(DragDeltaEventArgs e)
    {
      base.OnThumbDragDelta(e);
      this.SetToolTipContent();
    }

    /// <summary>
    /// Sets the right tooltip for the slider
    /// </summary>
    private void SetToolTipContent()
    {
      int index = Convert.ToInt32(this.AutoToolTip.Content.ToString());
      var content = Convert.ToString(ToolTips[index]);
      this.AutoToolTip.Content = content;
    }

    /// <summary>
    /// This retrieves the autoToolTip variable of the slider, which is not normally exposed
    /// </summary>
    private ToolTip AutoToolTip
    {
      get
      {
        if (_autoToolTip == null)
        {
          FieldInfo field = typeof(Slider).GetField(
            "_autoToolTip",
            BindingFlags.NonPublic | BindingFlags.Instance);

          _autoToolTip = field.GetValue(this) as ToolTip;
        }

        return _autoToolTip;
      }
    }

    /// <summary>
    /// Retrieves the Depth corresponding to each tick value
    /// </summary>
    public List<double> ToolTips
    {
      get
      {
        if (tooltipValues.Count == 0)
        {
          tooltipValues = this.GetValue(SliderProperty.ToolTipProperty) as List<double>;
        }

        return tooltipValues;
      }
    }
  }
}
