using System;
using System.Windows;
using System.Windows.Media;

namespace MapLinkProApp
{
  /// <summary>
  /// Represents the Vertical Slice view
  /// </summary>
  public class VerticalCrossSection : CrossSectionPanel
  {
    protected override Tuple<double, double> GetXRange()
    {
      return new Tuple<double, double>(0, new Distance { StartLatLon = CrossSection.SliceStart, EndLatLon = CrossSection.SliceEnd }.DistanceInNautical());
    }

    /// <summary>
    /// This draws the vertical slice for a given slice within the grid
    /// </summary>
    /// <param name="drawingContext"></param>
    protected override void DrawSlice(DrawingContext drawingContext)
    {
      double cellWidth = GridRect.Width / (double)Grid.NumColumns;
      double cellTop = GridRect.Top;

      // Draw Vertical Slice
      for (int y = 0; y < Grid.NumRows; y++)
      {
        double cellHeight = GetCellHeight(y, GridRect.Height);

        for (int x = 0; x < Grid.NumColumns; x++)
        {
          var z = Grid.GetValue(x, y);
          if (z != null && z.Item1)
          {
            drawingContext.DrawRectangle(
              new System.Windows.Media.SolidColorBrush(Envitia.MapLink.Grids.Data.ColourScales.GlobalInstance.GetColor(CrossSection.Property, z.Item2)),
              (System.Windows.Media.Pen)null,
              new Rect(GridRect.Left + (x * cellWidth), cellTop, cellWidth + 1, cellHeight + 1));
          }
        }

        cellTop += cellHeight;
      }
    }
  }
}
