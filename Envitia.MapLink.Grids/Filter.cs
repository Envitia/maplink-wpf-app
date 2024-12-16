using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// Simple filter class that identifies values plus or minus some Range relative to a ReferenceValue.
  /// </summary>
  public class Filter
  {
    public double ReferenceValue { get; set; }
    public double Range { get; set; }
    public bool Include(double z)
    {
      return z >= (ReferenceValue + Range)
        || z <= (ReferenceValue - Range);
    }
  }
}
