using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// Loads an ASCII grid file
  /// </summary>
  public abstract class GridDataset
  {
    
    public char Delimiter { get; set; } = ' ';

    public DataGrid DataGrid { get; protected set; }

    public double NoData { get; set; } = Double.NaN;

    /// <summary>
    /// Reads Grid file dimensiona
    /// </summary>
    /// <param name="streamReader"></param>
    /// <returns></returns>
    protected abstract bool ReadDimensions(StreamReader streamReader);

    /// <summary>
    /// Read z values and stores them in the grid
    /// </summary>
    /// <param name="streamReader">grid file stream (it points to the right location where z values start in the grid file)</param>
    /// <returns></returns>
    protected abstract bool ReadDataGrid(StreamReader streamReader);

    /// <summary>
    /// Loads a grid file
    /// </summary>
    /// <param name="gridFile">Path to the grid file</param>
    /// <returns></returns>
    public virtual bool Load(string gridFile)
    {
      const Int32 BufferSize = 4096;
      var fileStream = System.IO.File.OpenRead(gridFile);

      bool readSome = false;
      using (var streamReader = new System.IO.StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
      {
        readSome = ReadDimensions(streamReader);
        readSome &= ReadDataGrid(streamReader);
      }

      return readSome;
    }
  }
}
