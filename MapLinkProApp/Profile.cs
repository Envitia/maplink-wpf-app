using Envitia.MapLink.Grids;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media;

namespace MapLinkProApp.Data
{
  class Profile
  {
    public DataGrid Grid { get; set; }

    public List<double> ValidDepths = new List<double>();
    
    public List<Tuple<double, double>> GetValues(int x)
    {
      var profile = new List<Tuple<double, double>>(Grid.NumRows);

      for (int rowNum = 0; rowNum < Grid.NumRows; ++rowNum)
      {
        var val = Grid.GetValue(x, rowNum);
        if (val != null && val.Item1 && val.Item2 != 0.0 && !Double.IsNaN(val.Item2))
        {
          profile.Add(new Tuple<double, double>(Grid.Rows[rowNum], Grid.GetValue(x, rowNum).Item2));
          ValidDepths.Add(Grid.Rows[rowNum]);
        }
      }

      return profile;
    }

    /// <summary>
    /// Returns the Y (Depth) Range for the selected layer
    /// If no layers are selected, it returns the maximum depth configured
    /// </summary>
    /// <returns></returns>
    public double GetMaxY()
    {
      return ValidDepths.Count() > 0 ? ValidDepths.Max() : Grid.Rows.Last();
    }

    public static Tuple<double, double> MinMax(List<Tuple<double, double>> profile)
    {
      var min = Double.MaxValue;
      var max = Double.MinValue;

      foreach (var value in profile)
      {
        min = value.Item2 < min ? value.Item2 : min;
        max = value.Item2 > max ? value.Item2 : max;
      }

      return new Tuple<double, double>(min, max);
    }

    public static Tuple<double, double> MinMaxWithMargin(List<Tuple<double, double>> profile, double marginPercent = 10.0)
    {
      var minMax = MinMax(profile);

      double margin = (minMax.Item2 - minMax.Item1) / 100.0 * marginPercent;
      return new Tuple<double, double>(minMax.Item1 - margin, minMax.Item2 + margin);
    }

    /// Calculates the pixel y position offset for a real y value.
    private double ToYPosition(double depth, double gridHeight)
    {
      double yExtent = GetMaxY();
      double oneYUnit = gridHeight / yExtent;
      return (oneYUnit * depth);
    }

    // Calculates the pixel x position offset for a real x value.
    private double ToXPosition(double x, double minX, double maxX, double gridWidth)
    {
      double adjustedX = x - minX;
      double xExtent = maxX - minX;
      double oneXUnit = gridWidth / xExtent;
      return (oneXUnit * adjustedX);
    }

    public void Draw(int x, System.Windows.Rect gridRect, System.Windows.Media.DrawingContext drawingContext, string property, double xScaleMin = Double.NaN, double xScaleMax = Double.NaN)
    {
      var profileValues = GetValues(x);

      if (Double.IsNaN(xScaleMin) || Double.IsNaN(xScaleMax))
      {
        var minMax = MinMaxWithMargin(profileValues);
        if (Double.IsNaN(xScaleMin))
          xScaleMin = minMax.Item1;
        if (Double.IsNaN(xScaleMax))
          xScaleMax = minMax.Item2;
      }

      for (var i = 1; i < profileValues.Count; ++i)
      {
        var previous = profileValues[i - 1];
        var current = profileValues[i];

        var previousX = gridRect.Left + ToXPosition(previous.Item2, xScaleMin, xScaleMax, gridRect.Width);
        var previousY = gridRect.Top + ToYPosition(previous.Item1, gridRect.Height);

        var currentX = gridRect.Left + ToXPosition(current.Item2, xScaleMin, xScaleMax, gridRect.Width);
        var currentY = gridRect.Top + ToYPosition(current.Item1, gridRect.Height);

        var colour = Envitia.MapLink.Grids.Data.ColourScales.GlobalInstance.GetColor(property, current.Item2);

        drawingContext.DrawLine(new System.Windows.Media.Pen(new SolidColorBrush(colour), 3), 
          new System.Windows.Point(previousX, previousY), new System.Windows.Point(currentX, currentY));
      }
    }

  }
}
