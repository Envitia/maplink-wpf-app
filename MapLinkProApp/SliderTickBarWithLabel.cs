using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MapLinkProApp
{
  /// <summary>
  /// This class is designed to add a label for your slider tick
  /// </summary>
  public class SliderTickBarWithLabel : TickBar
  {
    private Maps Maps { get; set; } = new Maps();

    /// <summary>
    /// This generates the formatted text to be used as the label for slider ticks
    /// </summary>
    /// <param name="dc"></param>
    protected override void OnRender(DrawingContext dc)
    {
        Size size = new Size(ActualWidth, ActualHeight);
        int tickCount = (int)((Maximum - Minimum) / TickFrequency) + 1;
        if ((Maximum - Minimum) % TickFrequency == 0)
        {
          tickCount -= 1;
        }

        size.Height -= this.ReservedSpace;
        double tickFrequencySize = (size.Height * this.TickFrequency / (this.Maximum - this.Minimum));

        // Draw each tick text
        for (int i = 0, j = tickCount; i <= tickCount; i++, j--)
        {
          int index = Math.Max(0, Convert.ToInt32(Maximum) - Convert.ToInt32(Minimum + TickFrequency * i));
          // Ticks are set at the indices of depth array
          double depth = Maps.AllDepths[index];
          string text = Convert.ToString(Convert.ToInt32(depth), 10);

          FormattedText formattedText = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, 
                              new Typeface("Segoe UI"), 12, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip)
          {
            TextAlignment = TextAlignment.Right
          };

          dc.DrawText(formattedText, new Point(-10, (tickFrequencySize * (IsDirectionReversed ? j : i)) + (this.ReservedSpace * 0.5) - (formattedText.Width / 2)));
        }

        // Make sure tick is still shown
        base.OnRender(dc);
    }
  }
}
