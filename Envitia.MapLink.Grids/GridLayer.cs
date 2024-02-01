using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// A GridLayer visualises a DataGrid as a geo-referenced rectangular grid.
  /// Renders the grid into an off-screen bitmap which is then rendered to the drawing surface using the data layers's rendering interface.
  /// </summary>
  public class GridLayer : Envitia.MapLink.TSLNClientCustomDataLayer
  {
    /// <summary>
    /// The data grid to visualise.
    /// </summary>
    public DataGrid Grid { get; set; }

    /// <summary>
    /// The underlying custom data layer.
    /// </summary>
    public Envitia.MapLink.TSLNCustomDataLayer DataLayer { get; } = new TSLNCustomDataLayer();

    /// <summary>
    /// A maximum height in pixels for the rendered bitmap.
    /// The bitmap will be created with a height equalling this value or the grid's row count,
    /// whichever is smaller.
    /// Defaults to 800.
    /// </summary>
    public int MaxBitmapHeight { get; set; } = 800;
    
    /// <summary>
    /// A maximum width in pixels for the rendered bitmap.
    /// The bitmap will be created with a width equalling this value or the grid's column count,
    /// whichever is smaller.
    /// Defaults to 1200.
    /// </summary>
    public int MaxBitmapWidth { get; set; } = 1200;

    private System.Drawing.Bitmap Bitmap { get; set; }

    public GridLayer()
    {
      // Link the custom data layer to the client layer (this).
      DataLayer.setClientCustomDataLayer(this);
    }

    /// <summary>
    /// Maps a cell value to a pixel colour.
    /// Override this to provide other colour mappings.
    /// </summary>
    /// <param name="val">The cell value</param>
    /// <returns>The mapped colour.</returns>
    public virtual System.Drawing.Color ValueToColour(double val)
    {
      // TODO: Use some colour configuration
      var color = Data.ColourScales.GlobalInstance.GetColor("Temperature", val);

      if (color == null)
      {
        return System.Drawing.Color.FromArgb(255, System.Drawing.Color.White);
      }
      else
      {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
      }
    }

    /// <summary>
    /// Get the pixel value for the given lat/long coordinate.
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <returns>Tuple: item1: true if there is a value for this coordinate, false if not (i.e. empty pixel); double: the pixel value.</returns>
    public virtual Tuple<bool, double> GetPixelValue(double longitude, double latitude)
    {
      return Grid?.GetClosestValue(longitude, latitude);
    }

    /// <summary>
    /// Get the TMC envelope of the grid on the drawing surface.
    /// The method is used to determine whether the grid's extent intersects with the viewable extent, and therefore whether to bother with any rendering.
    /// </summary>
    /// <param name="drawingSurface"></param>
    /// <returns>The grid's TMC rendering extent on the given drawing surface.</returns>
    public virtual Envitia.MapLink.TSLNEnvelope GetEnvelope(Envitia.MapLink.TSLNDrawingSurface drawingSurface)
    {
      return Grid?.GetEnvelope(drawingSurface);
    }

    /// <summary>
    /// Create the off-screen bitmap.
    /// </summary>
    /// <param name="drawingSurface">The drawing surface the bitmap will be rendered into.</param>
    /// <param name="duBottomLeft">(xy) device unit coordinates for the bottom left of the bitmap.</param>
    /// <param name="duTopRight">(xy) device unit coordinates for the top right of the bitmap.</param>
    /// <returns>The off-screen bitmap.</returns>
    /// <exception cref="ArgumentNullException">The method throws an exception if the drawing surface is null.</exception>
    private System.Drawing.Bitmap CreateBitmap(Envitia.MapLink.TSLNDrawingSurface drawingSurface, Tuple<int, int> duBottomLeft, Tuple<int, int> duTopRight)
    {
      if (drawingSurface == null)
        throw new ArgumentNullException("drawingSurface");

      // Get the bitmap's dimensions
      int width = Math.Abs(duTopRight.Item1 - duBottomLeft.Item1) + 1;
      int height = Math.Abs(duTopRight.Item2 - duBottomLeft.Item2) + 1;
      int bitmapWidth = Math.Min(width, MaxBitmapWidth);
      int bitmapHeight = Math.Min(height, MaxBitmapHeight);

      // Pixel size in device units
      double pixelStepX = width / (double)bitmapWidth;
      double pixelStepY = height / (double)bitmapHeight;

      // Create a new off-screen, 32 bits-per-pixel, RGB bitmap with alpha channel for transparency.
      var bitmap = new System.Drawing.Bitmap(bitmapWidth, bitmapHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

      // Iterate over the pixels along x axis
      for (int x = 0; x < bitmapWidth; ++x)
      {
        // The x-axis pixel in device units
        int dux = (int)(duBottomLeft.Item1 + (x * pixelStepX));

        // Iterate over the pixels along the y axis
        for (int y = 0; y < bitmapHeight; ++y)
        {
          // The y-axis pixel in device units
          int duy = (int)(duBottomLeft.Item2 + (y * pixelStepY));

          // Device units to lat/long coordinate. TODO: faster to use TMC coordinates rather than lat/long?
          drawingSurface.DUToLatLong(dux, duy, out double latitude, out double longitude);

          // Get the grid value for this pixel.
          var closestVal = GetPixelValue(longitude, latitude);

          // Is there a value at this pixel? If not, leave the pixel unset.
          if (closestVal != null
            && closestVal.Item1
            && closestVal.Item2 >= 0)
          {
            // Set the pixel's value.
            bitmap.SetPixel(x, bitmapHeight - y - 1, ValueToColour(closestVal.Item2));
          }
        }
      }

      return bitmap;
    }

    /// <summary>
    /// Force a re-creation of the bitmap so that any changes are picked up.
    /// </summary>
    public void Reset()
    {
      Bitmap = null;
    }

    private TSLNEnvelope PreviousDrawnEnvelope { get; set; }

    private bool IsCreateNeeded(TSLNEnvelope renderingExtent)
    {
      if (Bitmap == null)
        return true;

      if (PreviousDrawnEnvelope == null)
        return true;

      // Maintain a reasonable rendering quality at different zoom levels without recreating the bitmap on every render.
      if (renderingExtent.width > PreviousDrawnEnvelope.width * 2)
        return true;
      if (renderingExtent.width < PreviousDrawnEnvelope.width * .5)
        return true;

      return false;
    }

    /// <summary>
    /// Implementation of TSLNClientCustomDataLayer.drawLayer().
    /// Renders the grid to an off-screen bitmap which is then rendered to the surface using the data layer's rendering interface.
    /// </summary>
    /// <param name="renderingInterface">The layer's rendering interface.</param>
    /// <param name="extent">Display area extent.</param>
    /// <param name="layerHandler">Data layer stuff, including the drawing surface we are rendering into.</param>
    /// <returns></returns>
    public override bool drawLayer(TSLNRenderingInterface renderingInterface, TSLNEnvelope extent, TSLNCustomDataLayerHandler layerHandler)
    {
      if (layerHandler.drawingSurface == null)
      {
        return false;
      }

      // Does the grid intersect with the display area?
      var gridEnvelope = GetEnvelope(layerHandler.drawingSurface);      
      if (!renderingInterface.extent.intersect(gridEnvelope))
      {
        // Not an error, just an optimisation.
        return true;
      }

      // Convert the grid extent from TMC to device units.
      layerHandler.drawingSurface.TMCToDU(gridEnvelope.bottomLeft.x, gridEnvelope.bottomLeft.y, out int bottomLeftDux, out int bottomLeftDuy);
      layerHandler.drawingSurface.TMCToDU(gridEnvelope.topRight.x, gridEnvelope.topRight.y, out int topRightDux, out int topRightDuy);

      // Make sure the devie unit extent is the right way round.
      if (bottomLeftDux > topRightDux)
      {
        (bottomLeftDux, topRightDux) = (topRightDux, bottomLeftDux);
      }
      if (bottomLeftDuy > topRightDuy)
      {
        (bottomLeftDuy, topRightDuy) = (topRightDuy, bottomLeftDuy);
      }

      // Only create the bitmap when absolutely necessary.
      if (IsCreateNeeded(renderingInterface.extent))
      {
        // Create the off-screen bitmap.
        Bitmap = CreateBitmap(layerHandler.drawingSurface, new Tuple<int, int>(bottomLeftDux, bottomLeftDuy), new Tuple<int, int>(topRightDux, topRightDuy));
        if (Bitmap == null)
          return false;

        // Remember this extent - used by IsCreateNeeded().
        PreviousDrawnEnvelope = renderingInterface.extent;
      }

      // Draw the bitmap using the layer's rendering interface.
      renderingInterface.graphics().DrawImage(Bitmap, bottomLeftDux, topRightDuy, topRightDux - bottomLeftDux, bottomLeftDuy - topRightDuy);

      // All good.
      return true;
    }    
  }
}
