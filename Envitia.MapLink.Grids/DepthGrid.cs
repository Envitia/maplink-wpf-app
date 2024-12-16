using System;
using System.Collections.Generic;
using System.Windows;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// A DataGrid where each row is a depth.
  /// </summary>
  public class DepthGrid
  {
    public DataGrid Grid { get; private set; }

    private const int NUM_COLUMNS = 50;

    /// <summary>
    /// Construct a depth grid from a set of terrain database.
    /// </summary>
    /// <param name="depthValues">Depths (in metres) + the terrain database for each depth.</param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    public DepthGrid(List<Tuple<double, Envitia.MapLink.Terrain.TSLNTerrainDatabase>> depthValues, double x1, double y1, double x2, double y2)
    {
      List<double> depths = new List<double>();
      foreach (var depth in depthValues)
      {
        depths.Add(depth.Item1);
      }

      for (int y = 0; y < depthValues.Count; ++y)
      {
        Envitia.MapLink.Terrain.TSLNTerrainDataItem[] dataItems = new Envitia.MapLink.Terrain.TSLNTerrainDataItem[NUM_COLUMNS];
        
        if (depthValues[y].Item2 != null 
          && depthValues[y].Item2.queryLine(x1,y1,x2,y2, NUM_COLUMNS, out dataItems) == Envitia.MapLink.Terrain.TSLNTerrainReturn.TSLNTerrain_OK)
        {
          if (Grid == null)
          {
            List<double> columns = new List<double>();
            for (int n = 0; n < dataItems.Length; ++n)
            {
              columns.Add(dataItems[n].m_x);              
            }

            Grid = new DataGrid(depths.ToArray(), columns.ToArray());
          }

          for (int x = 0; x < NUM_COLUMNS; ++x)
          {
            Grid.SetValue(x, y, dataItems[x].m_z);
          }
        }
      }
    }

    /// <summary>
    /// Construct a depth grid from a set of DataGrids
    /// </summary>
    /// <param name="depthValues">Depths (in metres) + the xy Grid for each depth.</param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    public DepthGrid(List<Tuple<double, DataGrid>> depthValues, double x1, double y1, double x2, double y2)
    {
      List<double> depths = new List<double>();
      foreach (var depth in depthValues)
      {
        depths.Add(depth.Item1);
      }

      Point startCoord = new Point(x1, y1);
      Point endCoord = new Point(x2, y2);
      Line line = new Line(startCoord, endCoord);
      Point[] points = line.getPoints(NUM_COLUMNS);
      List<double> columns = new List<double>();

      Point firstPoint = startCoord;
      foreach(var point in points)
      {
        // columns of the DataGrid are the distance (m) along the line [ Start Coord, End Coord ]
        columns.Add(TSLNCoordinateConverter.greatCircleDistance(firstPoint.X, firstPoint.Y, point.X, point.Y));
      }

      for (int y = 0; y < depthValues.Count; ++y)
      {
        List<double> dataItems = new List<double>(NUM_COLUMNS);

        if (depthValues[y].Item2 != null)
        {
          if(Grid == null)
          {
            Grid = new DataGrid(depths.ToArray(), columns.ToArray(), depthValues[y].Item2.GridBounds);
          }
          foreach (var point in points)
          {
            var closestValue = depthValues[y].Item2.GetClosestValue(point.X, point.Y);
            // Is there a value at this pixel? If not, leave the pixel unset.
            if (closestValue != null
              && closestValue.Item1
              && closestValue.Item2 >= 0)
            {
              dataItems.Add(closestValue.Item2);
            }
            else
            {
              // The point could be out of range for the Grid and we don't want to show this on the graph
              dataItems.Add(double.NaN);
            }
          }

          for (int x = 0; x < NUM_COLUMNS; ++x)
          {
            Grid.SetValue(x, y, dataItems[x]);
          }
        }
      }
    }
  }
}
