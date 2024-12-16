using System;
using System.Collections.Generic;
using System.Linq;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// Models an x,y,z grid of data.
  /// </summary>
  public class DataGrid
  {
    public double[] Rows { get; private set; }
    public double[] Columns { get; private set; }

    public int NumRows { get => Rows.Length; }
    public int NumColumns { get => Columns.Length; }

    public double MinZ { get; private set; } = Double.MaxValue;
    public double MaxZ { get; private set; } = Double.MinValue;

    public Bounds GridBounds { get; private set; }

    private Tuple<bool, double>[,] Grid { get; set; }

    public DataGrid()
    {
    }

    public DataGrid(double[] rows, double[] columns)
    {
      Rows = rows;
      Columns = columns;

      Grid = new Tuple<bool, double>[NumRows, NumColumns];
    }

    public DataGrid(double[] rows, double[] columns, Bounds bounds) : this(rows, columns)
    {
      this.GridBounds = bounds;
    }

    public Tuple<bool, double> GetValue(int x, int y)
    {
      if (x >= NumColumns)
      {
        throw new ArgumentOutOfRangeException("x");
      }
      if (y >= NumRows)
      {
        throw new ArgumentOutOfRangeException("y");
      }

      return Grid[y,x];
    }

    public class Range
    {
      public double TripValue { get; set; }
      public int StartIndex { get; set; }
      public int EndIndex { get; set; }

      public List<double> Trips { get; set; }

      public double GetTrip(Tuple<bool, double> value)
      {
        if (value == null || !value.Item1)
        {
          return Double.NaN;
        }

        for (int i = 1; i < Trips.Count; ++i)
        {
          if (value.Item2 >= Trips[i-1] && value.Item2 < Trips[i])
          {
            return Trips[i-1];
          }

          if (i == Trips.Count - 1
            && value.Item2 >= Trips[i])
          {
            return Trips[i];
          }
        }
        return Double.NaN;
      }
    }

    public class Bounds
    {
      public double MinX { get; set; } = Double.NaN;
      public double MaxX { get; set; } = Double.NaN;
      public double MinY { get; set; } = Double.NaN;
      public double MaxY { get; set; } = Double.NaN;

      List<double> bounds;

      public Bounds(double minX, double maxX, double minY, double maxY)
      {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;

        bounds = new List<double>() { minX, minY, maxX, maxY};
      }

      public bool IsWithinBounds(double x, double y)
      {
        if (!bounds.Contains(Double.NaN))
        {
          // Make sure we have genuine values for Min and Max (These are set only if they are present in the header)
          return (x >= MinX && y >= MinY && x <= MaxX && y <= MaxY);
        }
        // If we haven't got the right min and max values, we just allow all values
        return true;
      }
    }



    /// <summary>
    /// Get a list of ranges along the axes that sit between trip values.
    /// </summary>
    /// <param name="y">The row to examine.</param>
    /// <param name="trips">The list of triplines that separate the requested ranges.</param>
    /// <returns>List of X ranges.</returns>
    public List<Range> GetXRanges(int y, List<double> trips)
    {
      trips.Sort();

      var ranges = new List<Range>();
      
      var currentRange = new Range { StartIndex = 0, Trips = trips };
      var currentValue = GetValue(0, y);
      currentRange.TripValue = currentRange.GetTrip(currentValue);

      for (int x = 1; x < NumColumns; ++x)
      {        
        currentValue = GetValue(x, y);
        var trip = currentRange.GetTrip(currentValue);
        // Have we crossed one of the trips?
        if (Double.IsNaN(trip) && Double.IsNaN(currentRange.TripValue))
        {
          currentRange.StartIndex = x;
        }
        else if (trip != currentRange.TripValue)
        {
          if (currentRange.StartIndex < currentRange.EndIndex)
          {
            ranges.Add(currentRange);
          }

          // Start a new range at the next index
          currentRange = new Range { StartIndex = x, Trips = trips, TripValue = trip };
        }
        else
        {
          if (Double.IsNaN(currentRange.TripValue))
          {
            currentRange.StartIndex = x;
          }
          currentRange.EndIndex = x;
        }
      }

      if (!Double.IsNaN(currentRange.TripValue))
      {
        // Add the final range
        ranges.Add(currentRange);
      }

      return ranges;
    }

    public static int ClosestTo(double[] collection, double target)
    {
      // NB Method will return int.MaxValue for a sequence containing no elements.
      // Apply any defensive coding here as necessary.
      var closest = 0;
      var minDifference = double.MaxValue;
      bool minDifferenceSet = false;
      for (int i = 0; i < collection.Length; ++i)
      {
        var difference = Math.Abs(collection[i] - target);
        if (minDifference > difference)
        {
          minDifference = difference;
          closest = i;
          minDifferenceSet = true;
        }
        else if (minDifferenceSet && difference > minDifference)
        {
          // arrays are ordered low to high, so we know we can return now.
          return closest;
        }
      }

      return closest;
    }

    public Tuple<bool, double> GetClosestValue(double xVal, double yVal)
    {
      if ((bool)!GridBounds?.IsWithinBounds(xVal, yVal)) return null;

      if (xVal >= Columns.Last())
      {
        //throw new ArgumentOutOfRangeException("x");
      }
      if (yVal >= Rows.Last())
      {
        //throw new ArgumentOutOfRangeException("y");
      }

      int closestXIndex = ClosestTo(Columns, xVal);
      int closestYIndex = ClosestTo(Rows, yVal);

      return GetValue(closestXIndex, closestYIndex);
    }

    public Tuple<bool, double> SetValue(int x, int y, double value)
    {
      if (x >= NumColumns)
      {
        throw new ArgumentOutOfRangeException("x");
      }
      if (y >= NumRows)
      {
        throw new ArgumentOutOfRangeException("y");
      }

      MinZ = value < MinZ ? value : MinZ;
      MaxZ = value > MaxZ ? value : MaxZ;

      return Grid[y, x] = new Tuple<bool, double>(true, value);
    }

    /// <summary>
    /// Get the profile at a given x position. Profile is the value of each row at a given x position.
    /// </summary>
    /// <param name="x"></param>
    /// <returns>The value at each row at the x position.</returns>
    public List<Tuple<double, double>> GetProfile(int x)
    {
      var profile = new List<Tuple<double, double>>(NumRows);

      for (int rowNum = 0; rowNum < NumRows; ++rowNum)
      {
        var val = GetValue(x, rowNum);
        if (val != null && val.Item1 && val.Item2 != 0.0)
        {
          profile.Add(new Tuple<double, double>(Rows[rowNum], val.Item2));
        }
      }

      return profile;
    }

    /// <summary>
    /// Populate the DataGrid from a MapLink terrain database.
    /// </summary>
    /// <param name="terrainDatabase"></param>
    /// <param name="numColumns"></param>
    /// <param name="numRows"></param>
    /// <returns></returns>
    public bool FromTerrainDatabase(Envitia.MapLink.Terrain.TSLNTerrainDatabase terrainDatabase, int numColumns, int numRows)
    {
      double x1 = 0.0;
      double x2 = 0.0;
      double y1 = 0.0;
      double y2 = 0.0;

      if (terrainDatabase.queryExtent(out x1, out y1, out x2, out y2) != Envitia.MapLink.Terrain.TSLNTerrainReturn.TSLNTerrain_OK)
      {
        return false;
      }

      var dataItems = new Envitia.MapLink.Terrain.TSLNTerrainDataItem[numColumns * numRows];
      if (terrainDatabase.queryArea(x1, y1, x2, y2, numColumns, numRows, out dataItems) != Envitia.MapLink.Terrain.TSLNTerrainReturn.TSLNTerrain_OK)
      {
        return false;
      }

      Rows = new double[numRows];
      Columns = new double[numColumns];
      Grid = new Tuple<bool, double>[NumRows, NumColumns];

      for (int y = 0; y < numRows; ++y)
      {
        int terrainDatabaseY = numRows - (1 + y);
        for (int x = 0; x < numColumns; ++x)
        {
          var dataItem = dataItems[(terrainDatabaseY * numColumns) + x];
          if (y == 0)
          {
            Columns[x] = dataItem.m_x;
          }
          if (x == 0)
          {
            Rows[y] = dataItem.m_y;
          }
          Grid[y, x] = new Tuple<bool, double>(!dataItem.m_isNull, dataItem.m_z);
        }
      }

      return true;
    }

    public Envitia.MapLink.TSLNEnvelope GetEnvelope(Envitia.MapLink.TSLNDrawingSurface drawingSurface)
    {
      if (drawingSurface == null)
        return null;

      if (Columns.Length < 3)
        return null;
      if (Rows.Length < 3)
        return null;

      var tmcLeft = 0;
      var tmcBottom = 0;
      drawingSurface.latLongToTMC(Rows[0], Columns[0], out tmcLeft, out tmcBottom);
      var tmcRight = 0;
      var tmcTop = 0;
      drawingSurface.latLongToTMC(Rows[Rows.Length-1], Columns[Columns.Length-1],out tmcRight, out tmcTop);

      var tmcLeftInternal = 0;
      var tmcBottomInternal = 0;
      drawingSurface.latLongToTMC(Rows[1], Columns[1], out tmcLeftInternal, out tmcBottomInternal);

      // Using the centre of the adjacent cell, find the border between the cells.
      int xDistance = System.Math.Abs(tmcLeftInternal - tmcLeft);
      int yDistance = System.Math.Abs(tmcBottomInternal - tmcBottom);
      tmcLeft -= (int)((xDistance / 2.0) + 0.5);
      tmcBottom -= (int)((yDistance / 2.0) + 0.5);

      var tmcRightInternal = 0;
      var tmcTopInternal = 0;
      drawingSurface.latLongToTMC(Rows[Rows.Length - 2], Columns[Columns.Length - 2], out tmcRightInternal, out tmcTopInternal);

      xDistance = System.Math.Abs(tmcRight - tmcRightInternal);
      yDistance = System.Math.Abs(tmcTop - tmcTopInternal);
      tmcRight += (int)((xDistance / 2.0) + 0.5);
      tmcTop += (int)((yDistance / 2.0) + 0.5);

      return new Envitia.MapLink.TSLNEnvelope(tmcLeft, tmcBottom, tmcRight, tmcTop);
    }
  }
}
