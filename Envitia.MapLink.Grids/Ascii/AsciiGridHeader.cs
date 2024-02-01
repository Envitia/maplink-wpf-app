using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// Represents the header of an ASCII Grid File
  /// </summary>
  public struct AsciiGridHeader
  {
    public enum ByteOrder
    {
      LSBFirst,
      MSBFirst,
      VMS_FFloat
    };

    public double MinX { get; set; }

    public double MinY { get; set; }

    public double MaxX { get; set; }

    public double MaxY { get; set; }

    public int NumX { get; set; }

    public int NumY { get; set; }

    public ByteOrder Byteorder { get; set; }

    public double NullVal { get; set; }

    public bool YCenter { get; set; }
    public bool XCenter { get; set; }
    public double Cellsize { get; set; }
  }
}
