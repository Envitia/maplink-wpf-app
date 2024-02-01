using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.ComponentModel;

namespace MapLinkProApp
{
  internal class LayerSelector
  {
    private const String BORDER_PREFIX = "Border_";
    private const String BUTTON_PREFIX = "Button_";
    private const String LABEL_PREFIX = "Label_";
    private const String LAYER_OPTIONS_PREFIX = "LayerOption_";
    private const String LAYER_PREFIX = "Layer_";
    private const int TRANSPARENT_INDEX = 1;
    private const int DUPLICATE_INDEX = 2;

    // Rpresents the layers to be added to the layer selector
    public record struct LayerDetails
    {
      // Unique name of the layer, as configured in Maplink
      public string Name { get; init; }
      // Layer Property from the configuration
      public string Label { get; init; }
      // Index at which the layer is to be inserted (-1 for default)
      public int Index { get; set; }
    };

    private Window parentWindow;

    // This stores the labels of layers that are switched on
    private HashSet<LayerDetails> activeLayers = new HashSet<LayerDetails>();

    public DrawingSurfacePanel.MapViewerPanel MapPanel { get; set; }

    public string SelectedLayer { get; set; }

    public event EventHandler LayerChanged;

    public LayerSelector(Window parentWindow)
    {
      this.parentWindow = parentWindow;
    }

    /// <summary>
    /// Returns the first active layer. This is useful in determining which depths to display in the depth selector
    /// </summary>
    /// <returns></returns>
    public string GetActiveLayer()
    {
      return activeLayers.Count() > 0 ? activeLayers.First().Name : null;
    }

    /// <summary>
    /// This removes a given layer from the layer selection panel. It also removes any displayed overlays from the map
    /// prior to removing it fromthe selection panel
    /// </summary>
    /// <param name="layersPanel"></param>
    /// <param name="layerNames">List of layer names to be removed</param>
    /// <returns>List of indexes from which the elements are removed. -1 if nothing is removed</returns>
    public List<int> RemoveLayerFromSelectionPanel(StackPanel layersPanel, List<LayerDetails> layers)
    {
      List<int> removedIndexes = new List<int>();
      layers.ForEach(layer => { 
        showLayerOnMap(false, layer.Name);
        string layerId = GenerateIdFromLayerName(LAYER_PREFIX, layer.Name);
        var result = FindChild<StackPanel>(layersPanel, layerId);

        if (result.Item2 != null)
        {
          layersPanel.Children.Remove(result.Item2);
          removedIndexes.Add(result.Item1);
        }
        else
        {
          throw new KeyNotFoundException("Layer " + layer.Name + " not found");
        }
      }); 
      return removedIndexes;
    }

    /// <summary>
    /// This creates the layer dropdown and layer options controls and adds them to the layer selection panel
    /// </summary>
    /// <param name="layersPanel"></param>
    /// <param name="layer">Dictionary that maps layer name to layer label</param>
    public void AddLayerToSelectionPanel(StackPanel layersPanel, List<LayerDetails> layers)
    {
      layers.ForEach(layer => {
        Button layerButton = createLayerButton(layer);
        // Add click event to it so that it can show layer options panel
        layerButton.Name = GenerateIdFromLayerName(BUTTON_PREFIX, layer.Name);
        layerButton.Click += new RoutedEventHandler(LayerButton_Click);

        StackPanel layerOptionsPanel = new StackPanel();
        layerOptionsPanel.Visibility = Visibility.Collapsed;
        layerOptionsPanel.Orientation = Orientation.Vertical;
        layerOptionsPanel.Name = GenerateIdFromLayerName(LAYER_OPTIONS_PREFIX, layer.Name);

        if(MapPanel != null)
        { 
          // Transparency is set on a Map and we wouldn't be able to set transparency if we are not displaying the map
          AddItemToLayerOption(layerOptionsPanel, "Transparent", layer.Name, TRANSPARENT_INDEX);
        }

        layerOptionsPanel.Margin = new Thickness(5);

        Border layerOptionsBorder = new Border();
        layerOptionsBorder.Name = GenerateIdFromLayerName(BORDER_PREFIX, layer.Name);
        Style borderStyle = parentWindow.FindResource("StackBorder") as Style;
        layerOptionsBorder.Style = borderStyle;
        layerOptionsBorder.Child = layerOptionsPanel;

        StackPanel layerPanel = new StackPanel();
        layerPanel.Orientation = Orientation.Vertical;
        layerPanel.Margin = new Thickness(15, 0, 15, 10);
        layerPanel.Name = GenerateIdFromLayerName(LAYER_PREFIX, layer.Name);
        layerPanel.Children.Add(layerButton);
        layerPanel.Children.Add(layerOptionsBorder);
        // We set layername here so that it can be retrieved on event handling methods later
        layerPanel.SetValue(LayerProperty.LayerNameProperty, layer.Name);

        if (layer.Index == -1)
        {
          layersPanel.Children.Add(layerPanel);
        }
        else
        {
          layersPanel.Children.Insert(layer.Index, layerPanel);
        }

        // Switch on the layer if it was already selected to be displayed
        if (activeLayers.Where(item => item.Label.Equals(layer.Label)).Count () > 0)
        {
          activateLayer(layer, (StackPanel)layerButton.Content);
        }
      });
    }

    /// <summary>
    /// Sets the layer selection checkbox and switches on the corresponding layer on the Map
    /// </summary>
    /// <param name="layer">Details of the layer to be turned on</param>
    /// <param name="layerButtonPanel">Panel containing the layer checkbox</param>
    private void activateLayer(LayerDetails layer, StackPanel layerButtonPanel)
    {
      string layerId = GenerateIdFromLayerName("", layer.Name);
      CheckBox layerCheckbox = FindChild<CheckBox>(layerButtonPanel, layerId).Item2;
      layerCheckbox.IsChecked = true;

      // Turn on the layer on the Map
      showLayerOnMap(true, layer.Name);
    }

    /// <summary>
    /// This creates the layer dropdown control. The control consists of a checkbox, label and dropdown image added to a button
    /// </summary>
    /// <param name="layerName">This is used to name the control so that we can derive the layer name from control name at a later stage</param>
    /// <returns>layer dropdown control</returns>
    private Button createLayerButton(LayerDetails layer)
    {
      string layerName = layer.Name;
      CheckBox checkbox = new CheckBox();
      checkbox.VerticalAlignment = VerticalAlignment.Center;
      checkbox.Margin = new Thickness(5);
      checkbox.Name = GenerateIdFromLayerName("", layerName);
      checkbox.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(LayerCheckBoxChanged));

      Image icon = new Image
      {
        Height = 20,
        Width = 20,
        Source = new BitmapImage(new Uri("../img/drop-down.png", UriKind.Relative)),
        VerticalAlignment = VerticalAlignment.Center
      };
      Label label = new Label();
      label.Width = 150;
      label.Name = GenerateIdFromLayerName(LABEL_PREFIX, layerName);
      label.Content = layer.Label;

      StackPanel layerButtonPanel = new StackPanel();
      layerButtonPanel.Orientation = Orientation.Horizontal;
      layerButtonPanel.VerticalAlignment = VerticalAlignment.Center;
      layerButtonPanel.Children.Add(checkbox);
      layerButtonPanel.Children.Add(label);
      layerButtonPanel.Children.Add(icon);

      layerButtonPanel.SetValue(LayerProperty.LayerNameProperty, layerName);

      Button layerButton = new Button();
      Style style = parentWindow.FindResource("overlayButton") as Style;
      layerButton.Style = style;
      layerButton.Content = layerButtonPanel;

      return layerButton;
    }

    /// <summary>
    /// Adds the various checkboxes to the layer options panel
    /// </summary>
    /// <param name="parentElement">Parent element to which the checkbox would be added</param>
    /// <param name="labelContent"></param>
    /// <param name="layerName">Layer name is used to name the label so that we can derive the layername from element name at later stage</param>
    /// <param name="index">this is used to create distinct names for each checkbox</param>
    private void AddItemToLayerOption(StackPanel parentElement, string labelContent, string layerName, int index)
    {
      StackPanel stackPanel = new StackPanel();
      stackPanel.Orientation = Orientation.Horizontal;
      stackPanel.Name = GenerateIdFromLayerName(LAYER_OPTIONS_PREFIX + index, layerName);
      // The layer name is set here so that it can be retrieved later from event handling 
      // functions of its children
      stackPanel.SetValue(LayerProperty.LayerNameProperty, layerName);

      CheckBox checkbox = new CheckBox();
      Label label = new Label();
      label.Name = LAYER_OPTIONS_PREFIX + index;
      label.Content = labelContent;

      if (index == 1)
      {
        checkbox.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(TransparentCheckBoxChanged));
      }
      else
      {
        checkbox.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(DuplicateCheckBoxChanged));
      }

      stackPanel.Children.Add(checkbox);
      stackPanel.Children.Add(label);

      parentElement.Children.Add(stackPanel);
    }

    /// <summary>
    /// Event handler for layer dropdown, to expand it so that layer options are visible
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LayerButton_Click(object sender, RoutedEventArgs e)
    {
      Button button = (Button)sender;
      StackPanel stackPanel = (StackPanel)button.Parent;

      string layerName = GetLayerNameFromProperty(stackPanel);
      string layerOptionsName = GenerateIdFromLayerName(LAYER_OPTIONS_PREFIX, layerName);

      StackPanel layerOptionsPanel = FindChild<StackPanel>(stackPanel, layerOptionsName).Item2;
      Visibility visibility = layerOptionsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
      layerOptionsPanel.Visibility = visibility;
    }

    /// <summary>
    /// Checkbox event handler function for the transparency functionality
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TransparentCheckBoxChanged(object sender, RoutedEventArgs e)
    {
      CheckBox checkBox = (CheckBox)sender;
      StackPanel parent = (StackPanel)checkBox.Parent;

      string layerName = GetLayerNameFromProperty(parent);

      string labelName = LAYER_OPTIONS_PREFIX + TRANSPARENT_INDEX;
      Label label = FindChild<Label>(parent, labelName).Item2;

      // This would apply styling to checkbox (different styles depending on whether it is checked or not
      setCheckBoxColor(checkBox, label);

      setTransparency(checkBox.IsChecked, layerName);
    }

    /// <summary>
    /// Styles checkbox and associated text (different styles depending on if it is selected or not)
    /// </summary>
    /// <param name="checkBox"></param>
    /// <param name="checkBoxLabel"></param>
    private void setCheckBoxColor(CheckBox checkBox, Label checkBoxLabel)
    {
      Color textColor = (Color)parentWindow.FindResource("CheckboxSelectedTextColor");
      Brush labelBrushSelected = new SolidColorBrush(textColor);

      if ((bool)checkBox.IsChecked == true)
      {
        Color checkBoxColor = (Color)parentWindow.FindResource("CheckboxCheckedColor");
        Brush checkBoxBrush = new SolidColorBrush(checkBoxColor);
        checkBox.Background = checkBoxBrush;
        checkBoxLabel.Foreground = labelBrushSelected;
      }
      else
      {
        Color checkBoxColor = (Color)parentWindow.FindResource("CheckboxDefaultColor");
        Brush checkBoxBrush = new SolidColorBrush(checkBoxColor);
        checkBox.Background = checkBoxBrush;
        checkBoxLabel.Foreground = Brushes.Black;
      }
    }

    /// <summary>
    /// This doesn't do anything at the moment, but it is the event handler for handing the copy to vertical slice 
    /// view when we add that one.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DuplicateCheckBoxChanged(object sender, RoutedEventArgs e)
    {
      CheckBox checkBox = (CheckBox)sender;
      StackPanel stackPanel = (StackPanel)checkBox.Parent;

      string layerName = GetLayerNameFromProperty(stackPanel);
      string labelName = LAYER_OPTIONS_PREFIX + DUPLICATE_INDEX;
      Label label = FindChild<Label>(stackPanel, labelName).Item2;
      //TODO: show layer in Second Plan Window

      // This would apply styling to checkbox (different styles depending on whether it is checked or not
      setCheckBoxColor(checkBox, label);
    }

    /// <summary>
    /// Even handler for layer checkbox - shows or hides an overlay
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LayerCheckBoxChanged(object sender, RoutedEventArgs e)
    {
      CheckBox checkBox = (CheckBox)sender;
      StackPanel stackPanel = (StackPanel)checkBox.Parent;

      string layerName = GetLayerNameFromProperty(stackPanel);
      string labelName = GenerateIdFromLayerName(LABEL_PREFIX, layerName);
      Label label = FindChild<Label>(stackPanel, labelName).Item2;

      setCheckBoxColor(checkBox, label);

      if (MapPanel != null)
      {
        showLayerOnMap(checkBox.IsChecked, layerName);
      }

      // Store the visible layers so that we can switch them back on when changing the depth
      if ((bool)checkBox.IsChecked)
      {
        activeLayers.Add(new LayerDetails { Name = layerName, Label = label.Content.ToString() });
      }
      else
      {
        activeLayers.Remove(new LayerDetails { Name = layerName, Label = label.Content.ToString() });
      }

      // Store the Selected Layer property so that it can be used by the xz views
      SelectedLayer = label.Content.ToString();
      OnLayerChanged(EventArgs.Empty);
    }

    /// <summary>
    /// This would extract layername stored as a property of the given element. It expects the element name to be "prefix+layername"
    /// </summary>
    /// <param name="stackPanel">Element where the layername property is saved</param>
    /// <returns></returns>
    private string GetLayerNameFromProperty(StackPanel stackPanel)
    {
      return stackPanel.GetValue(LayerProperty.LayerNameProperty) as string;
    }

    /// <summary>
    /// Generate an appropriate ID that can be stored as the element name from the given parameters
    /// </summary>
    /// <param name="prefix">Any prefix to be added to the layer name</param>
    /// <param name="layerName">Layer name</param>
    /// <returns>The generated ID that can be used as the 'Name' property of the given WPF control</returns>
    private string GenerateIdFromLayerName(string prefix, string layerName)
    {
      string id = layerName.Replace(" ", "_");
      return prefix + id.Replace(".", "_");
    }

    /// <summary>
    /// Event handler that is invoked when the selected layer is changed
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnLayerChanged(EventArgs e)
    {
      LayerChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Show or hide a given layer based on whether the layer is selected or not
    /// </summary>
    /// <param name="visible">Whether to show or hide the layer</param>
    /// <param name="layerName">Layer name</param>
    private void showLayerOnMap(bool? visible, string layerName)
    {
      MapPanel.DrawingSurface.setDataLayerProps(layerName, Envitia.MapLink.TSLNPropertyEnum.TSLNPropertyVisible, (bool)visible ? 1 : 0);
      MapPanel.DrawingSurface.redraw();
    }

    /// <summary>
    /// Set the transparency property of a given layer
    /// </summary>
    /// <param name="visible">Whether we want the layer transparent or not</param>
    /// <param name="layerName">Layer name</param>
    private void setTransparency(bool? visible, string layerName)
    {
      MapPanel.DrawingSurface.setDataLayerProps(layerName, Envitia.MapLink.TSLNPropertyEnum.TSLNPropertyTransparency, (bool)visible ? 100 : 255);
      MapPanel.DrawingSurface.redraw();
    }

    /// 
    /// Finds a Child of a given item in the visual tree. (This is a utility method. So this can be moved to a utility class 
    /// and made static so that it is accessible from other classes as well in the future)
    /// </summary>
    /// <param name="parent">A direct parent of the queried item.</param>
    /// <typeparam name="T">The type of the queried item.</typeparam>
    /// <param name="childName">x:Name or Name of child. </param>
    /// <returns>The first parent item that matches the submitted type parameter, along with the index where the item is found. 
    /// If not matching item can be found, 
    /// a null parent is being returned.</returns>
    private static Tuple<int, T> FindChild<T>(DependencyObject parent, string childName)
         where T : DependencyObject
    {
      // Confirm parent and childName are valid. 
      if (parent == null) return null;

      T foundChild = null;
      int childIndex = -1;

      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        // If the child is not of the request child type child
        T childType = child as T;
        if (childType == null)
        {
          // recursively drill down the tree
          Tuple<int, T> result = FindChild<T>(child, childName);
          foundChild = result.Item2 as T;

          // If the child is found, break so we do not overwrite the found child. 
          if (foundChild != null)
          {
            childIndex = i;
            break;
          }
        }
        else if (!string.IsNullOrEmpty(childName))
        {
          var frameworkElement = child as FrameworkElement;
          // If the child's name is set for search
          if (frameworkElement != null && frameworkElement.Name == childName)
          {
            // if the child's name is of the request name
            foundChild = (T)child;
            childIndex = i;
            break;
          }
        }
        else
        {
          // child element found.
          foundChild = (T)child;
          childIndex = i;
          break;
        }
      }

      return new Tuple<int, T>(childIndex, foundChild);
    }
  }
}
