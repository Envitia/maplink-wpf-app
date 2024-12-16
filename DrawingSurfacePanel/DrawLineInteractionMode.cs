using System;
using Envitia.MapLink;
using Envitia.MapLink.InteractionModes;
using Envitia.MapLink.OpenGLSurface;

namespace DrawingSurfacePanel
{
  /// <summary>
  /// This class demonstrates how to implement a custom interaction mode
  /// </summary>
  public class DrawLineInterationMode : TSLNInteractionMode
  {
    public interface IObserver
    {
      void Invalidate();
      void NewLine(Tuple<double, double> startMu, Tuple<double, double> endMu);
    }
    public System.Collections.Generic.List<IObserver> Observers { get; } = new System.Collections.Generic.List<IObserver>();

    public int ID { get; }

    public TSLNCoord StartCoord { get; set; }
    public TSLNCoord EndCoord { get; set; }

    public bool IsActive { get; private set; }

    public TSLNStandardDataLayer Overlay { get; } = new TSLNStandardDataLayer();

    private TSLNPolyline Line { get; set; }

    enum FeatureIDs
    {
      FeatureStartEllipse = 6786876,
      FeatureEndEllipse,
      FeatureLine
    }

    public DrawLineInterationMode(int id)
      : base(id, false)
    {
      ID = id;
    }

    private static void SetSymbolRendering(TSLNEntity symbol, bool isStart)
    {
      symbol.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeSymbolStyle, 3);
      symbol.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeSymbolColour, isStart ? 19 : 1);
      symbol.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeSymbolMinPixelSize, 20);
      symbol.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeSymbolMaxPixelSize, 20);
      symbol.setRendering(Envitia.MapLink.TSLNRenderingAttributeDouble.TSLNRenderingAttributeEdgeThickness, 3);
      symbol.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeEdgeColour, System.Drawing.Color.FromArgb(150, 255, 255, 255).ToArgb());
    }


    private static void SetLineRendering(Envitia.MapLink.TSLNEntity line)
    {
      if (line != null)
      {
        line.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeEdgeStyle, 7);
        line.setRendering(Envitia.MapLink.TSLNRenderingAttributeDouble.TSLNRenderingAttributeEdgeThickness, 1);
        line.setRendering(Envitia.MapLink.TSLNRenderingAttributeInt.TSLNRenderingAttributeEdgeColour, System.Drawing.Color.FromArgb(255, 0, 0, 0).ToArgb());
      }
    }

    private TSLNSymbol AddSymbol(int featureID, TSLNCoord location)
    {
      var symbol = Overlay.entitySet.createSymbol(featureID, location.x, location.y);
      SetSymbolRendering(symbol, true);
      return symbol;
    }

    private TSLNPolyline AddLine(TSLNCoord start, TSLNCoord end)
    {
      var lineCoords = new TSLNCoordSet();
      lineCoords.add(start);
      lineCoords.add(end);
      var line = Overlay.entitySet.createPolyline((int)FeatureIDs.FeatureLine, lineCoords);
      SetLineRendering(line);

      return line;
    }

    public void Initialise()
    {
      AddSymbol((int)FeatureIDs.FeatureStartEllipse, StartCoord);
      AddSymbol((int)FeatureIDs.FeatureEndEllipse, EndCoord);
      AddLine(StartCoord, EndCoord);
    }

    public override bool onLButtonDown(int x, int y, bool shift, bool control)
    {
      if (this.display != null && this.display.drawingSurfaceBase != null)
      {
        IsActive = true;

        var ds = (TSLNDrawingSurface)this.display.drawingSurfaceBase;
        int tmcX = 0;
        int tmcY = 0;
        ds.DUToTMC(x, y, out tmcX, out tmcY);

        StartCoord = new TSLNCoord( tmcX, tmcY);

        Overlay.entitySet.clear();

        AddSymbol((int)FeatureIDs.FeatureStartEllipse, StartCoord);
        Line = AddLine(StartCoord, new TSLNCoord(StartCoord.x + 50, StartCoord.y + 50));

        Overlay.notifyChanged();

        this.display.viewChanged(true);
        display.drawingSurface.redraw();

        foreach (var observer in Observers)
        {
          observer.Invalidate();
        }

        return true;
      }
      return false;
    }

    public override bool onMouseMove(TSLNButtonType button, int x, int y, bool shift, bool control)
    {
      if (!IsActive)
        return base.onMouseMove(button, x, y, shift, control);

      var ds = (TSLNDrawingSurface)this.display.drawingSurfaceBase;
      int tmcX = 0;
      int tmcY = 0;
      ds.DUToTMC(x, y, out tmcX, out tmcY);

      var lineCoords = new TSLNCoordSet();
      lineCoords.add(StartCoord);
      lineCoords.add(tmcX, tmcY);
      Line.points(lineCoords);

      Overlay.notifyChanged();
      this.display.viewChanged(true);

      foreach (var observer in Observers)
      {
        observer.Invalidate();
      }

      return true;
    }

    public override bool onLButtonUp(int x, int y, bool shift, bool control)
    {
      if (!IsActive)
        return base.onLButtonUp(x, y, shift, control);

      IsActive = false;

      var ds = (TSLNDrawingSurface)this.display.drawingSurfaceBase;
      int tmcX = 0;
      int tmcY = 0;
      ds.DUToTMC(x, y, out tmcX, out tmcY);
      EndCoord = new TSLNCoord(tmcX, tmcY);

      AddSymbol((int)FeatureIDs.FeatureEndEllipse, EndCoord);

      var lineCoords = new TSLNCoordSet();
      lineCoords.add(StartCoord);
      lineCoords.add(tmcX, tmcY);
      Line.points(lineCoords);

      double startMuX = 0;
      double startMuY = 0;
      ds.TMCToMU(StartCoord.x, StartCoord.y, out startMuX, out startMuY);
      var start = new Tuple<double, double>(startMuX, startMuY);
      double endMuX = 0;
      double endMuY = 0;
      ds.TMCToMU(EndCoord.x, EndCoord.y, out endMuX, out endMuY);
      var end = new Tuple<double, double>(endMuX, endMuY);

      Overlay.notifyChanged();
      this.display.viewChanged(true);

      foreach (var observer in Observers)
      {
        observer.Invalidate();
        observer.NewLine(start, end);
      }

      return true;
    }

    public override Envitia.MapLink.TSLNCursorStyle queryCursor()
    {
      return TSLNCursorStyle.TSLNCursorStyleMovePoint;
    }

    public override string queryPrompt()
    {
      return "Draw a line to select depth slice";
    }

  }
}
