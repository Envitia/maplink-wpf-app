using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// Specialisation of GridLayer that draws values from a series of radial grids propagating out from a central lat/long coordinate.
  /// </summary>
  public class RadialGridLayer : GridLayer
  {
    /// <summary>
    /// The latitudinal (y) coordinate of the central point.
    /// </summary>
    public double CentreLatitude { get; set; }
    /// <summary>
    /// The longitudinal (x) coordinate of the central point.
    /// </summary>
    public double CentreLongitude { get; set; }

    /// <summary>
    /// Tuple returned by GetPixelValue() indicating a pixel with no grid value.
    /// </summary>
    public static Tuple<bool, double> NoResult { get; } = new Tuple<bool, double>(false, Double.NaN);

    /// <summary>
    /// The radial grids. This class does not use the GridLayer.Grid property.
    /// The double field is the radial's bearing, in degrees, from the central point.
    /// Each DataGrid should have XZ axes, with the X axis being metres from the central point, and the Z axis being some other property (e.g. depth or height).
    /// </summary>
    private SortedDictionary<double, DataGrid> Radials { get; set; }

    private double[] Bearings { get; set; }

    /// <summary>
    /// The maximum distance from the central point that the radial grids extend to.
    /// </summary>
    public double MaxDistance()
    {
      var maxDistance = Double.MinValue;
      foreach (var bearing in Radials)
      {
        maxDistance = Math.Max(maxDistance, bearing.Value.Columns.Max());
      }
      return maxDistance;
    }

    public RadialGridLayer()
    {
    }

    /// <summary>
    /// Set the radial grids. This class does not use the GridLayer.Grid property.
    /// </summary>
    /// <param name="radialGrids">The double field is the radial's bearing, in degrees, from the central point.
    /// Each DataGrid should have XZ axes, with the X axis being metres from the central point, and the Z axis being some other property (e.g. depth or height).</param>
    public void SetRadials(SortedDictionary<double, DataGrid> radialGrids)
    {
      Radials = new SortedDictionary<double, DataGrid>();

      foreach (var radial in radialGrids)
      {
        Radials.Add(radial.Key, radial.Value);

        // Duplicate the grid at a full circumference, to ensure the closest radial to a bearing is always found
        // (e.g. so that a bearing of 359 matches a bearing specifed as 0, not 290).
        Radials.Add(radial.Key + 360.0, radial.Value);
      }

      // Cache all the bearings.
      Bearings = Radials.Keys.ToArray();
    }

    /// <summary>
    /// Get the radial grid whose bearing is closest to the given bearing.
    /// </summary>
    /// <param name="bearing">The bearing in degrees to find.</param>
    /// <returns>The radial grid whose bearing is closest to the given bearing.</returns>
    private DataGrid ClosestRadialGrid(double bearing)
    {
      int closestIndex = DataGrid.ClosestTo(Bearings, bearing);
      if (closestIndex == Double.MaxValue)
      {
        return null;
      }

      return Radials[Bearings[closestIndex]];
    }

    /// <summary>
    /// Override of GridLayer.GetEnvelope.
    /// Calculated the extent based upon the maximum distances that the radials extend out from the centre.
    /// </summary>
    /// <param name="drawingSurface"></param>
    /// <returns>Bounding rectangular envelope of all the radials.</returns>
    public override TSLNEnvelope GetEnvelope(TSLNDrawingSurface drawingSurface)
    {
      var maxDistance = MaxDistance();

      // Calculate the furthest points from the centre at each compass point.
      double dummy = 0;
      TSLNCoordinateConverter.greatCircleDistancePoint(CentreLatitude, CentreLongitude, 0, maxDistance, out double north, out dummy);
      TSLNCoordinateConverter.greatCircleDistancePoint(CentreLatitude, CentreLongitude, 90, maxDistance, out dummy, out double east);
      TSLNCoordinateConverter.greatCircleDistancePoint(CentreLatitude, CentreLongitude, 180, maxDistance, out double south, out dummy);
      TSLNCoordinateConverter.greatCircleDistancePoint(CentreLatitude, CentreLongitude, 270, maxDistance, out dummy, out double west);

      // Convert the lat/long coordinates into TMCs
      drawingSurface.latLongToTMC(south, west, out int tmcLeft, out int tmcBottom);
      drawingSurface.latLongToTMC(north, east, out int tmcRight, out int tmcTop);

      // Create the bounding rectangular envelope.
      return new TSLNEnvelope(new TSLNCoord(tmcLeft, tmcBottom), new TSLNCoord(tmcRight, tmcTop));
    }

    /// <summary>
    /// Override of GridLayer.GetPixelValue().
    /// Get the pixel value for the given lat/long coordinate. The pixel value is found using the distance and bearing of the coordinate from the central point,
    /// getting the closest radial grid to the bearing, and finally getting the closest XZ-axis value from the radial grid.
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <returns>Tuple: item1: true if there is a value for this coordinate, false if not (i.e. empty pixel); double: the pixel value.
    /// Returns NoResult if the coordinates do not locate a pixel value.</returns>
    public override Tuple<bool, double> GetPixelValue(double longitude, double latitude)
    {
      // Get the distance and bearing of this coordinate from the central point.
      Envitia.MapLink.TSLNCoordinateConverter.greatCircleDistance(CentreLatitude, CentreLongitude, latitude, longitude, out double distance, out double bearing);
    
      // Check distance is within the maximum range.
      if (distance > MaxDistance())
      {
        return NoResult;
      }

      // Get the closest radial grid to the bearing.
      DataGrid radial = ClosestRadialGrid(bearing);
      if (radial == null)
      {
        return NoResult;
      }

      // Ask the grid for a pixel value.
      // TODO: The y value should be selectable (e.g. depth or height).
      return radial.GetClosestValue(distance, 10);
    }
  }
}
