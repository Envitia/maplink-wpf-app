using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Envitia.MapLink.Grids.Data
{
  public class ContourColour
  {
    public double MinZ { get; set; }
    public double MaxZ { get; set; }
    public double Z { get; set; }
    public System.Windows.Media.Color Color { get => System.Windows.Media.Color.FromArgb(A, R, G, B); }
    public byte A { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    
    public ContourColour(double z, byte r, byte g, byte b)
    {
      Z = z;
      R = r;
      G = g;
      B = b;
      A = 255;
    }

    public ContourColour(double z, byte r, byte g, byte b, byte a)
    {
      Z = z;
      R = r;
      G = g;
      B = b;
      A = a;
    }
  }

  public class ColourScales
  {
    private static ColourScales globalInstance = new ColourScales();
    public static ColourScales GlobalInstance { get { return globalInstance; } }

    private SortedDictionary<string, List<ContourColour>> propertyColours = new SortedDictionary<string, List<ContourColour>>();

    private enum Tokens
    {
      TokenZ,
      TokenR,
      TokenG,
      TokenB,

      NumTokens
    }

    public static ContourColour ClosestTo(IEnumerable<ContourColour> collection, double z)
    {
      return collection.OrderBy(x => Math.Abs(z - x.Z)).First();
    }

    public void Load(string property, string contourPath)
    {
      using (var reader = new System.IO.StreamReader(contourPath))
      {
        List<ContourColour> contourColours = new List<ContourColour>();

        while (!reader.EndOfStream)
        {
          var line = reader.ReadLine();
          var values = line.Split(',');

          if (values.Length == (int)Tokens.NumTokens)
          {
            contourColours.Add(new ContourColour(
              Convert.ToDouble(values[(int)Tokens.TokenZ]), 
              Convert.ToByte(values[(int)Tokens.TokenR]), 
              Convert.ToByte(values[(int)Tokens.TokenG]), 
              Convert.ToByte(values[(int)Tokens.TokenB])
              ));
          }
        }
        propertyColours[property] = contourColours;
      }
    }
    public System.Windows.Media.Color GetColor(string property, double zValue)
    {
      var colours = propertyColours[property];
      var closestColour = ClosestTo(colours, zValue);
      return closestColour.Color;
    }
  }
}
