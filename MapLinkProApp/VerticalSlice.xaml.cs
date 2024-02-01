using DrawingSurfacePanel;
using Envitia.MapLink.Grids;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapLinkProApp
{
  /// <summary>
  /// Interaction logic for Window1.xaml
  /// </summary>
  public partial class VerticalSliceWindow : Window
  {
    public DockableWindow Dockable { get; private set; }

    private LayerSelector LayerSelector { get; set; }

    public VerticalSliceWindow(DockableWindow dockable)
    {
      InitializeComponent();  

      Dockable = dockable;
      Dockable.Window = this;

      LayerSelector = new LayerSelector(this);
      LayerSelector.LayerChanged += OnLayerChanged;
    }

    /// <summary>
    /// Initialises vertical slice view
    /// </summary>
    public void Initialise(CrossSection crossSection)
    {
      StackPanel layersPanel = (StackPanel)this.FindName("VerticalLayersPanel");
      List<LayerSelector.LayerDetails> layers = new List<LayerSelector.LayerDetails>();
      // Layer selector shows all distinct layer properties with a valid depth
      crossSection.Maps.MapLayers.FindAll(x => x.Depth != Double.MaxValue).Select(x => x.Property).Distinct().ToList().ForEach(layer =>
      {
        layers.Add(new LayerSelector.LayerDetails{ Name = layer, Label = layer, Index = -1 });
      });
      LayerSelector.AddLayerToSelectionPanel(layersPanel, layers);

      VerticalSection.Initialise(crossSection);
    }

    private void Minimise_Click(object sender, RoutedEventArgs e)
    {
      Dockable.RaiseMinimiseEvent();
    }

    private void Dock_Click(object sender, RoutedEventArgs e)
    {
      Dockable.RaiseDockEvent();
    }

    private void Maximise_Click(object sender, RoutedEventArgs e)
    {
      Dockable.RaiseMaximiseEvent();
    }

    /// <summary>
    /// Show or hides the layer selection panel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LayersButton_Click(object sender, RoutedEventArgs e)
    {
      StackPanel element = (StackPanel)this.FindName("VerticalLayersPanel");

      Visibility visibility = element.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
      element.Visibility = visibility;
    }

    /// <summary>
    /// Handles notification when an overlay changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnLayerChanged(object sender, EventArgs e)
    {
      LayerSelector layerSelector = (LayerSelector)sender;
      string layer = layerSelector.SelectedLayer;

      VerticalSection.UpdateCrossSection(layer);
    }
  }
}
