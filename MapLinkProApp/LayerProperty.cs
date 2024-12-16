using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapLinkProApp
{
  internal class LayerProperty
  {
    public static readonly DependencyProperty LayerNameProperty = DependencyProperty.RegisterAttached(
      "SetLayerName", typeof(string), typeof(LayerProperty), new PropertyMetadata() );

    public static string GetLayerNameProperty(DependencyObject obj)
    {
      return (string)obj.GetValue(LayerNameProperty);
    }

    public static void SetLayerNameProperty(DependencyObject obj, string value)
    {
      obj.SetValue(LayerNameProperty, value);
    }
  }
}
