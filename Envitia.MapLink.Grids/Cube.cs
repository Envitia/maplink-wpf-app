using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Envitia.MapLink.Grids
{
  public class Cube
  {
    public class ZDimension
    {
      public int Z { get; set; }
      public DataGrid XY { get; } = new DataGrid();
    }

    public List<ZDimension> ZDimensions { get; } = new List<ZDimension>();

    public Tuple<bool, double> GetValue(int x, int y, int z)
    {
      if (ZDimensions.Count == 0)
      {
        throw new Exception("Cube has no dimensions");
      }

      var found = ZDimensions.Find(delegate (ZDimension dim)
      {
        return dim.Z == z;
      });

      if (found == null)
      {
        // Outside z range. Not an exception.
        return new Tuple<bool, double>(false, Double.NaN);
      }

      return found.XY.GetClosestValue(x, y);
    }
  }
}
