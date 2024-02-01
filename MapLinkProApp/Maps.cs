using System;
using System.Collections.Generic;
using System.Linq;

using System.Configuration;
using System.Xml;
using Envitia.MapLink.Grids;

namespace MapLinkProApp
{
  /// <summary>
  /// Map management.
  /// </summary>
  public class Maps
  {
    public List<MapLayers.MapLayer> MapLayers { get; set; } = new List<MapLayers.MapLayer>();

    /// <summary>
    /// Get the foundation (AKA base, background), layer
    /// </summary>
    public MapLayers.MapLayer FoundationLayer { get; private set; }

    public Maps()
    {
      // Load all map layers from a configuration XML file.
      var doc = new XmlDocument();
      var mapConfig = ConfigurationManager.AppSettings["mapLayersConfig"];
      doc.Load(mapConfig);

      // Load the MapLink map layers.
      var mapLayersNode = doc.SelectSingleNode("//MapLayers");
      var mapLayerNodes = mapLayersNode.SelectNodes("MapLinkMapLayer");
      foreach (XmlNode mapLayerNode in mapLayerNodes)
      {
        var mapLayer = new MapLayers.MapLinkMapLayer()
        {
          Depth = mapLayerNode.SelectSingleNode("Depth") == null ? MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS : Convert.ToDouble(mapLayerNode.SelectSingleNode("Depth").InnerText),
          Property = mapLayerNode.SelectSingleNode("Property").InnerText,
          FeatureType = mapLayerNode.SelectSingleNode("FeatureType").InnerText,
          DataLocation = Environment.ExpandEnvironmentVariables(mapLayerNode.SelectSingleNode("DataLocation").InnerText),
          TerrainLocation = mapLayerNode.SelectSingleNode("TerrainLocation") == null ? "" : mapLayerNode.SelectSingleNode("TerrainLocation").InnerText,
          IsFoundationLayer = mapLayerNode.SelectSingleNode("FoundationLayer") != null,
          Z = mapLayerNode.SelectSingleNode("Z") == null ? 0 : int.Parse(mapLayerNode.SelectSingleNode("Z").InnerText),
        };
        // TRhere could be multiple layers with the same properties, but for different layers
        // We are creating a unique layer name here
        mapLayer.Name = mapLayer.Depth == Double.MaxValue ? mapLayer.Property : mapLayer.Property + " " + mapLayer.Depth.ToString();

        if (mapLayer.IsFoundationLayer)
        {
          // Foundation layer goes at the bottom of the Z-order
          FoundationLayer = mapLayer;
          FoundationLayer.Z = -100;
        }
        else
        {
          MapLayers.Add(mapLayer);
        }
      }

      // Load all grid visualisations
      var gridNodes = mapLayersNode.SelectNodes("GridMapLayer");
      foreach (XmlNode gridNode in gridNodes)
      {
        var mapLayer = new MapLayers.AsciiGridMapLayer
        {
          Depth = gridNode.SelectSingleNode("Depth") == null ? MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS : Convert.ToDouble(gridNode.SelectSingleNode("Depth").InnerText),
          Property = gridNode.SelectSingleNode("Property").InnerText,
          FeatureType = gridNode.SelectSingleNode("FeatureType").InnerText,
          DataLocation = gridNode.SelectSingleNode("DataLocation").InnerText,
          TerrainLocation = gridNode.SelectSingleNode("TerrainLocation") == null ? "" : gridNode.SelectSingleNode("TerrainLocation").InnerText,
          Z = gridNode.SelectSingleNode("Z") == null ? 5 : int.Parse(gridNode.SelectSingleNode("Z").InnerText),
        };
        // TRhere could be multiple layers with the same properties, but for different layers
        // We are creating a unique layer name here
        mapLayer.Name = mapLayer.Depth == Double.MaxValue ? mapLayer.Property : mapLayer.Property + " " + mapLayer.Depth.ToString();

        MapLayers.Add(mapLayer);
      }

      // Load all native (Direct Import) map layers.
      var nativeLayerNodes = mapLayersNode.SelectNodes("NativeMapLayer");

      foreach (XmlNode nativeLayerNode in nativeLayerNodes)
      {
        var mapLayer = new MapLayers.NativeMapLayer()
        {
          Depth = nativeLayerNode.SelectSingleNode("Depth") == null ? MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS : Convert.ToDouble(nativeLayerNode.SelectSingleNode("Depth").InnerText),
          Property = nativeLayerNode.SelectSingleNode("Property").InnerText,
          FeatureType = nativeLayerNode.SelectSingleNode("FeatureType").InnerText,
          DataLocation = Environment.ExpandEnvironmentVariables(nativeLayerNode.SelectSingleNode("DataLocation").InnerText),
          TerrainLocation = nativeLayerNode.SelectSingleNode("TerrainLocation") == null ? "" : nativeLayerNode.SelectSingleNode("TerrainLocation").InnerText,
          Z = nativeLayerNode.SelectSingleNode("Z") == null ? 10 : int.Parse(nativeLayerNode.SelectSingleNode("Z").InnerText),
        };
        // TRhere could be multiple layers with the same properties, but for different layers
        // We are creating a unique layer name here
        mapLayer.Name = mapLayer.Depth == Double.MaxValue ? mapLayer.Property : mapLayer.Property + " " + mapLayer.Depth.ToString();

        if (mapLayer.IsFoundationLayer)
        {
          FoundationLayer = mapLayer;
          FoundationLayer.Z = -100;
        }
        else
        {
          MapLayers.Add(mapLayer);
        }
      }

      // Sort by Z-order.
      Sort();
    }

    /// <summary>
    /// Sort the layers by Z-order.
    /// </summary>
    public void Sort()
    {
      MapLayers.Sort(delegate (MapLayers.MapLayer first, MapLayers.MapLayer second)
      {
        if (first.Z == second.Z)
        {
          if (first.Depth == second.Depth)
          {
            return String.Compare(first.Property, second.Property);
          }
          return first.Depth < second.Depth ? -1 : 1;
        }          
        return first.Z < second.Z ? -1 : 1;
      });
    }

    /// <summary>
    /// Get the layer for the specified depth, property and feature type.
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="property"></param>
    /// <param name="featureType"></param>
    /// <returns>Map layer</returns>
    public MapLayers.MapLayer GetMapLayer(double depth, string property, string featureType)
    {
      var found = MapLayers.Find(delegate (MapLayers.MapLayer layer)
      {
        return layer.Depth == depth && layer.Property == property && layer.FeatureType == featureType;
      });

      return found;
    }

    public List<string> AllProperties
    {
      get
      {
        var properties = new List<string>();
        foreach (var layer in MapLayers)
        {
          properties.Add(layer.Property);
        }
        return properties.Distinct().ToList();
      }
    }

    public List<double> AllDepths
    {
      get
      {
        var depths = new List<double>();
        foreach (var layer in MapLayers)
        {
          if (layer.Depth != MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS)
          {
            depths.Add(layer.Depth);
          }
        }
        depths.Sort();
        return depths.Distinct().ToList();
      }
    }

    public List<string> AllFeatureTypes
    {
      get
      {
        var properties = new List<string>();
        foreach (var layer in MapLayers)
        {
          properties.Add(layer.FeatureType);
        }
        return properties.Distinct().ToList();
      }
    }

    public List<Tuple<double, Envitia.MapLink.Terrain.TSLNTerrainDatabase>> DepthValues(string property)
    {
      var depths = new List<Tuple<double, Envitia.MapLink.Terrain.TSLNTerrainDatabase>>();
      foreach (var layer in MapLayers)
      {
        if (layer.Depth != MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS
          && layer.Property == property
          && layer.GetTerrainDatabase() != null)
        {
          depths.Add(new Tuple<double, Envitia.MapLink.Terrain.TSLNTerrainDatabase>(layer.Depth, layer.GetTerrainDatabase()));
        }
      }
      depths.Sort(delegate (Tuple<double, Envitia.MapLink.Terrain.TSLNTerrainDatabase> first, Tuple<double, Envitia.MapLink.Terrain.TSLNTerrainDatabase> second)
      {
        if (first.Item1 == second.Item1) return 0;
        return first.Item1 < second.Item1 ? -1 : 1;
      });
      return depths;
    }

    // Returns a list of depths, along with the ascii files corresponding to each depth
    public List<Tuple<double, DataGrid>> DepthGridValues(string property)
    {
      var depths = new List<Tuple<double, DataGrid>>();
      foreach (var layer in MapLayers)
      {
        if (layer.Depth != MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS
          && layer.Property == property
          && layer.FeatureType == "Grid")
        {
          depths.Add(new Tuple<double, DataGrid>(layer.Depth, layer.GetDataGrid()));
        }
      }
      depths.Sort(delegate (Tuple<double, DataGrid> first, Tuple<double, DataGrid> second)
      {
        if (first.Item1 == second.Item1) return 0;
        return first.Item1 < second.Item1 ? -1 : 1;
      });
      return depths;
    }

    /// <summary>
    /// Returns the list of depths for a given DataGrid
    /// If it is not a DataGrid, it returns all depths in App config
    /// </summary>
    /// <param name="property">Selected Layer Property</param>
    /// <returns>List of depths for the given layer property</returns>
    public List<double> Depths(string property)
    {
      var depths = new List<double>();
      foreach (var layer in MapLayers)
      {
        if (layer.Depth != MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS
          && layer.Property == property
          && layer.FeatureType == "Grid"
          && layer.GetDataGrid().MaxZ != Double.MinValue
          && layer.GetDataGrid().MinZ  != Double.MaxValue)
        {
          depths.Add(layer.Depth);
        }
      }
      return depths.Count() == 0 ? AllDepths : depths;
    }

    /// <summary>
    /// Shows the map layers identified by the set of properties and feature types, at the given depth.
    /// </summary>
    /// <param name="surface"></param>
    /// <param name="properties"></param>
    /// <param name="featureTypes"></param>
    /// <param name="depth"></param>
    /// <param name="transparentOverlays">If true, set the layer opacities to less than 255.</param>
    public void ShowLayers(Envitia.MapLink.TSLNDrawingSurface surface, string[] properties, string[] featureTypes, double depth, bool transparentOverlays)
    {
      foreach (var layer in MapLayers)
      {
        bool visible = properties.Contains(layer.Property)
            && (layer.FeatureType.Length == 0 || featureTypes.Contains(layer.FeatureType))
            && (layer.Depth == MapLinkProApp.MapLayers.MapLayer.ALL_DEPTHS || layer.Depth == depth);

        if (visible)
        {
          var dataLayer = surface.getDataLayer(layer.Identifier());
          if (dataLayer == null)
          {
            surface.addDataLayer(layer.GetDataLayer(), layer.Identifier());
          }
        }

        layer.Opacity = transparentOverlays ? 120 : 255;
        layer.ConfigureMapLayer(surface, visible, depth);    
      }
    }

    public void ReorderLayers(Envitia.MapLink.TSLNDrawingSurface surface)
    {
      foreach (var layer in MapLayers)
      {
        surface.bringToFront(layer.Identifier());
      }
    }
  }
}
