using MapLinkProApp.MapLayers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MapLinkProApp
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public DockableWindow VerticalSliceDockable { get; set; } = new DockableWindow();
    public DockableWindow DepthProfileDockable { get; set; } = new DockableWindow();

    public DockManager SideDockManager { get; } = new DockManager { Location = DockManager.DockLocation.Right };

    public DockManager BottomDockManager { get; } = new DockManager { Location = DockManager.DockLocation.Bottom };

    private DrawingSurfacePanel.MapViewerPanel MapPanel { get; set; }

    private Maps Maps { get; set; } = new Maps();

    public double SelectedDepth { get; set; }

    private LayerSelector LayerSelector { get; set; }

    public MainWindow()
    {
      InitializeComponent();

      SideDockManager.DockPanel = SideDockPanel;
      SideDockManager.MaximiseGridLocation = new DockManager.GridLocation { Grid = this.MaximiseGrid, X = 0, Y = 0 };
      SideDockManager.FloatGridLocation = new DockManager.GridLocation { Grid = this.VertFloatingWindowGrid, X = 2, Y = 0 };

      BottomDockManager.DockPanel = BottomDockPanel;
      BottomDockManager.MaximiseGridLocation = new DockManager.GridLocation { Grid = this.MaximiseGrid, X = 0, Y = 0 };
      BottomDockManager.FloatGridLocation = new DockManager.GridLocation { Grid = this.HorzFloatingWindowGrid, X = 0, Y = 0 };

      Envitia.MapLink.TSLNCoordinateSystem.loadCoordinateSystems();

      MapPanel = (MainMap.Child as DrawingSurfacePanel.MapViewerPanel);

      LayerSelector = new LayerSelector(this);
      LayerSelector.MapPanel = MapPanel;

      ShowFoundationLayer();

      SetDepthSelectorValues();

      StackPanel layersPanel = (StackPanel)this.FindName("MainLayersPanel");
      Maps.MapLayers.ForEach(layer =>
      {
        layer.Panel = MapPanel;
        MapPanel.DrawingSurface.addDataLayer(layer.GetDataLayer(), layer.Name);
        MapPanel.DrawingSurface.setDataLayerProps(layer.Name, Envitia.MapLink.TSLNPropertyEnum.TSLNPropertyVisible, 0);
        // The layers defined in the config are hidden initially, but added to the layer selection panel so that
        // we can turn them on later. We want to add only the layers that are relevant for the selected depth
        if (layer.Depth == Double.MaxValue || SelectedDepth == layer.Depth)
        {
          LayerSelector.AddLayerToSelectionPanel(layersPanel, new List<LayerSelector.LayerDetails> { new LayerSelector.LayerDetails { Name = layer.Name, Label = layer.Property, Index = -1 } }.ToList());
        }
      });

      LoadColourConfig();
      InitialiseCrossSectionPanels();
    //  Maps.ReorderLayers(MapPanel.DrawingSurface);
    }

    /// <summary>
    /// Sets Depth Values on the Depth Selector
    /// </summary>
    private void UpdateDepthSelectorValues(string layer)
    {
      Slider depthSelector = this.FindName("DepthSelector") as Slider;

      var depths = Maps.Depths(layer);

      depthSelector.Minimum = 0;
      depthSelector.Maximum = depths.Count - 1;

      // We use depth indices as Slider ticks and Slider ToolTip would display
      // the actual tooltip
      depthSelector.SetValue(SliderProperty.ToolTipProperty, depths);

      if (SelectedDepth == 0)
      {
        SelectedDepth = depths[Convert.ToInt32(depthSelector.Value)];
      }
    }

    /// <summary>
    /// Event handler for the depth selection slider
    /// This will make sure only the layers corresponding to the selected depth are 
    /// displayed in the LayerSelector
    /// </summary>
    /// <param name="sender">Depth selection slider</param>
    /// <param name="e"></param>
    private void Depth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      Slider slider = (Slider)sender;

      if (SelectedDepth != 0)
      {
        var layersToRemove = Maps.MapLayers.Where(x => x.Depth == SelectedDepth)
                                            .Select(y => new LayerSelector.LayerDetails { Name = y.Name, Label = y.Property}).ToList();
        SelectedDepth = Maps.AllDepths[Convert.ToInt32(slider.Value)];

        StackPanel layersPanel = (StackPanel)this.FindName("MainLayersPanel");
        var newLayers = Maps.MapLayers.Where(x => x.Depth == SelectedDepth)
            .Select(y => new LayerSelector.LayerDetails { Name = y.Name, Label = y.Property, Index = -1}).ToList();

        var indexesRemoved = LayerSelector.RemoveLayerFromSelectionPanel(layersPanel, layersToRemove);
        int i = 0;
        // We need to insert the new layers at the same place as previous layers
        indexesRemoved.ForEach(index =>
        {
          newLayers[i] = newLayers[i] with { Index = index};
        });

        LayerSelector.AddLayerToSelectionPanel(layersPanel, newLayers);
      }
    }

    /// <summary>
    /// Sets Depth Values on the Depth Selector
    /// </summary>
    private void SetDepthSelectorValues()
    {
      Slider depthSelector = this.FindName("DepthSelector") as Slider;
      depthSelector.Minimum = 0;
      depthSelector.Maximum = Maps.AllDepths.Count - 1;

      // We use depth indices as Slider ticks and Slider ToolTip would display
      // the actual tooltip
      depthSelector.SetValue(SliderProperty.ToolTipProperty, Maps.AllDepths);

      SelectedDepth = Maps.AllDepths[Convert.ToInt32(depthSelector.Value)];
    }

    /// <summary>
    /// Initialises vertical slice to display the slice corresponding to the application configuration value
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void InitialiseCrossSectionPanels() 
    { 
      var slice = System.Configuration.ConfigurationManager.AppSettings["CrossSectionSlice"];
      var coordinates = slice.Split(',');
      if (coordinates.Length != 4)
      {
        throw new ArgumentOutOfRangeException("CrossSectionSlice configured incorrectly");
      }

      // Draw line represented by Vertical Slice
      CrossSection crossSection = new CrossSection(
          new Tuple<double, double>(Convert.ToDouble(coordinates[0]), Convert.ToDouble(coordinates[1])),
          new Tuple<double, double>(Convert.ToDouble(coordinates[2]), Convert.ToDouble(coordinates[3])),
          Maps,
          MapPanel,
          Maps.AllProperties[0]
        );
      Maps.FoundationLayer.GetDataLayer().coordinateSystem.MUToTMC(crossSection.SliceStart.Item1, crossSection.SliceStart.Item2, out int xTMC, out int yTMC);
      MapPanel.DrawLineInterationMode.StartCoord = new Envitia.MapLink.TSLNCoord(xTMC, yTMC);
      Maps.FoundationLayer.GetDataLayer().coordinateSystem.MUToTMC(crossSection.SliceEnd.Item1, crossSection.SliceEnd.Item2, out xTMC, out yTMC);
      MapPanel.DrawLineInterationMode.EndCoord = new Envitia.MapLink.TSLNCoord(xTMC, yTMC);
      MapPanel.DrawLineInterationMode.Initialise();

      VerticalSliceWindow verticalSlice = new VerticalSliceWindow(VerticalSliceDockable);
      VerticalSliceDockable.Minimised = this.VerticleSliceButton;
      SideDockManager.AddWindow(verticalSlice.Dockable);
      verticalSlice.Initialise(crossSection);

      DepthProfileDockable.Minimised = this.DepthProfileButton;
      DepthProfileWindow depthProfile = new DepthProfileWindow(DepthProfileDockable);
      SideDockManager.AddWindow(depthProfile.Dockable);
      depthProfile.Initialise(crossSection);
    }

    /// <summary>
    /// Reads the colour configuration from the settings
    /// </summary>
    private void LoadColourConfig()
    {
      // Load the colours to use from the app config file.
      var doc = new System.Xml.XmlDocument();
      doc.Load(System.Configuration.ConfigurationManager.AppSettings["ColourScales"]);

      var rootNode = doc.SelectSingleNode("//ColourScales");
      var coloursNodes = rootNode.SelectNodes("Colours");

      foreach (System.Xml.XmlNode mapLayerNode in coloursNodes)
      {
        var property = mapLayerNode.SelectSingleNode("Property").InnerText;
        var path = mapLayerNode.SelectSingleNode("Path").InnerText;

        Envitia.MapLink.Grids.Data.ColourScales.GlobalInstance.Load(property, path);
      }
    }

    /// <summary>
    /// Make sure the foundation layer is visible on the Map
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void ShowFoundationLayer()
    {
      if (Maps.FoundationLayer == null)
      {
        throw new ArgumentOutOfRangeException("Maps configured incorrectly");
      }

      Maps.FoundationLayer.Panel = MapPanel;
      MapPanel.SetFoundationLayer(Maps.FoundationLayer);
    }

    private void VerticleSliceButton_Click(object sender, RoutedEventArgs e)
    {
      VerticalSliceDockable.Window.Owner = this;
      VerticalSliceDockable.RaiseRestoreEvent();
    }

    private void DepthProfileButton_Click(object sender, RoutedEventArgs e)
    {
      DepthProfileDockable.Window.Owner = this;
      DepthProfileDockable.RaiseRestoreEvent();
    }

    /// <summary>
    /// Show or hides the layer selection panel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LayersButton_Click(object sender, RoutedEventArgs e)
    {
      StackPanel element = (StackPanel)this.FindName("MainLayersPanel");

      Visibility visibility = element.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
      element.Visibility = visibility;
    }

    /// <summary>
    /// Window Closed Event Handler
    /// </summary>
    /// <param name="e"></param>
    protected override void OnClosed(EventArgs e)
    {
      base.OnClosed(e);
      // Shut things down 2 seconds from now
      Timer t = new Timer(
          (state) => { App.Current.Shutdown(); },
          null, 2000, -1);
    }
  }
}
