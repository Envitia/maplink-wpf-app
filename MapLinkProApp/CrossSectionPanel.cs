using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DrawingSurfacePanel;
using Envitia.MapLink;
using Envitia.MapLink.Grids;

namespace MapLinkProApp
{
  public record struct CrossSection(Tuple<double, double> SliceStart, Tuple<double, double> SliceEnd, Maps Maps, MapViewerPanel MapPanel, string Property);

  /// <summary>
  /// DrawingVisualElement provides an element onto which the map can be drawn.
  /// </summary>
  public class DrawingVisualElement : FrameworkElement
  {
    private System.Windows.Media.VisualCollection _children;

    public System.Windows.Media.DrawingVisual drawingVisual;

    public DrawingVisualElement()
    {
      _children = new System.Windows.Media.VisualCollection(this);

      drawingVisual = new System.Windows.Media.DrawingVisual();
      _children.Add(drawingVisual);
    }

    protected override int VisualChildrenCount
    {
      get { return _children.Count; }
    }

    protected override System.Windows.Media.Visual GetVisualChild(int index)
    {
      if (index < 0 || index >= _children.Count)
        throw new ArgumentOutOfRangeException();

      return _children[index];
    }
  }

  /// <summary>
  /// CrossSectionPanel is the vertical slice (xz) view.
  /// The class implements DrawingSurfacePanel.DrawLineInterationMode.IObserver so that it can be notified when a new vertical slice is selected in the xy view.
  /// The panel draws a grid of data using .Net drawing classes. It does not use MapLink for drawing.
  /// </summary>
  public class CrossSectionPanel : System.Windows.Controls.Panel, DrawingSurfacePanel.DrawLineInterationMode.IObserver
  {
    public class Distance
    {
      const double MetresPerNauticalMile = 1852;

      public Tuple<double, double> StartLatLon { get; set; }
      public Tuple<double, double> EndLatLon { get; set; }

      public double DistanceInMetres()
      {
        return TSLNCoordinateConverter.greatCircleDistance(StartLatLon.Item2, StartLatLon.Item1, EndLatLon.Item2, EndLatLon.Item1);
      }

      public double DistanceInNautical()
      {
        return DistanceInMetres() / MetresPerNauticalMile;
      }
    }

    private DrawingVisualElement drawingVisualElement = new DrawingVisualElement();

    public DataGrid Grid { get; set; } = null;

    public double IndicatedDepth { get; set; } = Double.NaN;

    public Size GridSize { get; set; }

    public bool UniformRowHeights { get; set; } = false;

    public CrossSection CrossSection { get; private set; }

    protected Rect GridRect {  get; set; }

    public CrossSectionPanel()
    {
      base.Children.Add(drawingVisualElement);
    }

    public void Initialise(CrossSection crossSection)
    {
      CrossSection = crossSection;
      DepthGrid depthGrid = new DepthGrid(CrossSection.Maps.DepthGridValues(CrossSection.Property), CrossSection.SliceStart.Item1, CrossSection.SliceStart.Item2, CrossSection.SliceEnd.Item1, CrossSection.SliceEnd.Item2);
      Grid = depthGrid.Grid;

      CrossSection.MapPanel.DrawLineInterationMode.Observers.Add(this);
    }

    protected double GetCellHeight(int y, double gridHeight)
    {
      if (UniformRowHeights)
      {
        return y * (gridHeight / (double)Grid.NumRows);
      }

      double depthExtent = Grid.Rows.Last();
      double oneUnitHeight = gridHeight / depthExtent;
      double rowDifference = y > 0 ? Grid.Rows[y] - Grid.Rows[y - 1] : Grid.Rows[y];
      return (oneUnitHeight * rowDifference);
    }

    protected virtual void DrawSlice(DrawingContext drawingContext)
    {
      // Noop by default
    }

    protected virtual Tuple<double, double> GetXRange()
    {
      return new Tuple<double, double>(0, 0);
    }

    protected virtual Tuple<double, double> GetYRange()
    {
      return new Tuple<double, double>(CrossSection.Maps.AllDepths.Min(), CrossSection.Maps.AllDepths.Max());
    }

    public void Draw()
    {
      if (Grid == null)
      {
        return;
      }
      if (CrossSection.Property.Length == 0)
      {
        return;
      }
      if (GridSize.Width == 0 || GridSize.Height == 0)
      {
        return;
      }

      const int bigScaleMarginPixels = 50;
      const int smallScaleMarginPixels = 15;

      GridRect = new Rect(
          bigScaleMarginPixels, 
          bigScaleMarginPixels, 
          GridSize.Width - bigScaleMarginPixels - smallScaleMarginPixels, 
          GridSize.Height - bigScaleMarginPixels - smallScaleMarginPixels);

      var drawingContext = drawingVisualElement.drawingVisual.RenderOpen();

      DrawSlice(drawingContext);
      
      System.Windows.Media.Pen blackPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
      blackPen.DashStyle = System.Windows.Media.DashStyles.Solid;

      drawingContext.DrawLine(blackPen, GridRect.TopLeft, GridRect.TopRight);  // Line at the top

      drawingContext.DrawLine(blackPen, GridRect.BottomLeft, GridRect.BottomRight); // X-Coordinate

      const double numXCoords = 6.0;
      double xInterval = (GridRect.Right - GridRect.Left) / numXCoords; // Interval between x axis coordinates

      Tuple<double, double> xRange = GetXRange();

      double xIncrement = (xRange.Item2 - xRange.Item1) / numXCoords;  // Actual distance between each x value

      // Draw x-axis labels
      double xCoord = GridRect.Left;
      double xVal = xRange.Item1;
      for (int i = 0; i <= numXCoords; ++i)
      {
        Point coord = new Point(xCoord, GridRect.Bottom + 10);
        FormattedText text = new FormattedText(Math.Round(xVal, 1).ToString(), System.Globalization.CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight, 
          new Typeface("LillyUPC"), 10, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(text, coord);
        Point lineCoord1 = new Point(xCoord, GridRect.Bottom);
        Point lineCoord2 = new Point(xCoord, GridRect.Bottom + 10);
        drawingContext.DrawLine(blackPen, lineCoord1, lineCoord2);
        xCoord += xInterval;
        xVal += xIncrement;
      }

      drawingContext.DrawLine(blackPen, GridRect.BottomLeft, GridRect.TopLeft); // Y-Coordinate

      Tuple<double, double> yRange = GetYRange();

      // Draw Y-axis labels
      const int numYCoords = 6;
      double yInterval = (GridRect.Top - GridRect.Bottom) / numYCoords;
      double yCoord = GridRect.Bottom;
      double depth = yRange.Item2;
      double depthInterval = (yRange.Item2 - yRange.Item1) / numYCoords;
      for (int i = 0; i <= numYCoords; ++i)
      {
        Point coord = new Point(GridRect.Left - 40, yCoord);
        FormattedText text = new FormattedText(Math.Round(depth, 1).ToString(), System.Globalization.CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight,
          new Typeface("LillyUPC"), 10, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(text, coord);
        Point lineCoord1 = new Point(GridRect.Left, yCoord);
        Point lineCoord2 = new Point(GridRect.Left - 10, yCoord);
        drawingContext.DrawLine(blackPen, lineCoord1, lineCoord2);
        yCoord += yInterval;
        depth -= depthInterval;
      }

      drawingContext.Close();
    }

    /// <summary>
    /// Updates Depth Grid to match new layer and redraws the vertical slice
    /// </summary>
    /// <param name="layer"></param>
    public void UpdateCrossSection(string layer)
    {
      CrossSection = CrossSection with { Property = layer };
      UpdateCrossSection();
    }

    private void UpdateCrossSection()
    {
      DepthGrid depthGrid = new DepthGrid(
        CrossSection.Maps.DepthGridValues(CrossSection.Property),
        CrossSection.SliceStart.Item1,
        CrossSection.SliceStart.Item2,
        CrossSection.SliceEnd.Item1,
        CrossSection.SliceEnd.Item2);

      Grid = depthGrid.Grid;

      Draw();
    }

    private double roundValue(double value)
    {
      double rem = value % 10;
      return rem >= 5 ? (value - rem + 10) : (value - rem);
    }

    // Override the default Measure method of Panel
    protected override Size MeasureOverride(Size availableSize)
    {
      Size panelDesiredSize = new Size(300, 200);
      return panelDesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
      GridSize = finalSize;

      Draw();

      return finalSize; // Returns the final Arranged size
    }

    /// <summary>
    /// Handles notification when the Vertical Slice changes
    /// </summary>
    /// <param name="startMu">Start co-ordinate of the new slice in MU</param>
    /// <param name="endMu">End co-ordinate of the new slice in MU</param>
    void DrawLineInterationMode.IObserver.NewLine(Tuple<double, double> startMu, Tuple<double, double> endMu)
    {
      CrossSection.Maps.FoundationLayer.GetDataLayer().coordinateSystem.MUToLatLong(startMu.Item1, startMu.Item2, out double startLat, out double startLon);
      CrossSection.Maps.FoundationLayer.GetDataLayer().coordinateSystem.MUToLatLong(endMu.Item1, endMu.Item2, out double endLat, out double endLon);

      CrossSection = CrossSection with
      {
        SliceStart = new Tuple<double, double>(startLon, startLat),
        SliceEnd = new Tuple<double, double>(endLon, endLat)
      };
      UpdateCrossSection();
    }

    void DrawLineInterationMode.IObserver.Invalidate()
    {
    }
  }
}
