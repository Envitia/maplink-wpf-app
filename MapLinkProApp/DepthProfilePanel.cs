using Envitia.MapLink;
using Envitia.MapLink.Grids;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MapLinkProApp
{
  /// <summary>
  /// This class handles Depth Profile view
  /// The panel draws the depth profile of a grid of data using .Net drawing classes
  /// </summary>
  public class DepthProfilePanel : CrossSectionPanel
  {
    Data.Profile Profile { get; set; } = new Data.Profile();

    /// <summary>
    /// Calculates the pixel y position offset for a real y value.
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="gridHeight"></param>
    /// <returns></returns>
    private double ToYPosition(double depth, double gridHeight)
    {
      double yExtent = Grid.Rows.Last();
      double oneYUnit = gridHeight / yExtent;
      return (oneYUnit * depth);
    }

    /// <summary>
    /// Calculates the pixel x position offset for a real x value.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="gridWidth"></param>
    /// <returns></returns>
    private double ToXPosition(double x, double gridWidth)
    {
      double adjustedX = x - CrossSection.SliceStart.Item1;
      double xExtent = CrossSection.SliceEnd.Item1 - CrossSection.SliceStart.Item1;
      double oneXUnit = gridWidth / xExtent;
      return (oneXUnit * adjustedX);
    }

    /// <summary>
    /// Gets x-axis range. This would be the range of z-values for a given vertical slice
    /// </summary>
    /// <returns></returns>
    protected override Tuple<double, double> GetXRange()
    {
      return new Tuple<double, double>(Grid.MinZ, Grid.MaxZ);
    }

    /// <summary>
    /// Gets Y-axis range. This would range from 0 to the maximum depth for the vertical slice
    /// </summary>
    /// <returns></returns>
    protected override Tuple<double, double> GetYRange()
    {
      return new Tuple<double, double>(0, Profile.GetMaxY());
    }

    /// <summary>
    /// Draws the depth profile for a point on the vertical slice
    /// </summary>
    /// <param name="drawingContext"></param>
    protected override void DrawSlice(DrawingContext drawingContext)
    {
      Profile.Grid = Grid;

      var profileXPosMu = CrossSection.SliceStart.Item1 + ((CrossSection.SliceEnd.Item1 - CrossSection.SliceStart.Item1) / 3); // This should be selectable by the user
      var xIndex = DataGrid.ClosestTo(Grid.Columns, profileXPosMu);
      Profile.Draw(xIndex, GridRect, drawingContext, CrossSection.Property);

      Point profileStartPoint = new Point(
        GridRect.Left + ToXPosition(profileXPosMu, GridRect.Width),
        GridRect.Top + ToYPosition(Grid.Rows.First(), GridRect.Height));
      Point profileEndPoint = new Point(
        GridRect.Left + ToXPosition(profileXPosMu, GridRect.Width),
        GridRect.Top + ToYPosition(Grid.Rows.Last(), GridRect.Height));
    }
  }
}
