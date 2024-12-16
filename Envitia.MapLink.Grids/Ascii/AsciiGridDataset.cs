using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Envitia.MapLink.Grids.Ascii
{
  public class AsciiGridDataset : GridDataset
  {
    private const int PIXEL_OFFSET = 0;

    AsciiGridHeader header;

    /// <summary>
    /// Persists the ASCII header values
    /// </summary>
    /// <param name="noX"></param>
    /// <param name="noY"></param>
    /// <param name="blX"></param>
    /// <param name="blY"></param>
    /// <param name="noDataVal"></param>
    /// <param name="cellSize"></param>
    /// <param name="isBLLx"></param>
    /// <param name="isBLLy"></param>
    /// <returns></returns>
    private AsciiGridHeader populateHeader(int noX, int noY, double blX, double blY, double noDataVal, double cellSize, bool isBLLx, bool isBLLy)
    {
      header = new AsciiGridHeader();
      header.Cellsize = cellSize;
      header.XCenter = !isBLLx;
      if (isBLLx)
      {
        // Bottom left
        double cellsizeHalf = cellSize / 2.0;
        header.MinX = blX + cellsizeHalf;
        header.MaxX = (blX + cellSize * (noX - PIXEL_OFFSET)) + cellsizeHalf;
      }
      else
      {
        // center
        header.MinX = blX;
        header.MaxX = blX + cellSize * (noX - PIXEL_OFFSET);
      }
      header.YCenter = !isBLLy;
      if (isBLLy)
      {
        Double cellsizeHalf = cellSize / 2.0;
        header.MinY = blY + cellsizeHalf;
        header.MaxY = (blY + cellSize * (noY - PIXEL_OFFSET)) + cellsizeHalf;
      }
      else
      {
        header.MinY = blY;
        header.MaxY = blY + cellSize * (noY - PIXEL_OFFSET);
      }
      header.NumX = noX;
      header.NumY = noY;
      header.NullVal = noDataVal;

      return header;
    }

    /// <summary>
    /// Creates the DataGrid
    /// This doesn't handle xCenter / yCenter values just yet
    /// </summary>
    /// <param name="xllCorner"></param>
    /// <param name="yllCorner"></param>
    /// <param name="cellSize"></param>
    /// <param name="isBllX">Whether we have an xCorner or xCenter in the ascii file</param>
    /// <param name="isBllY">Whether we have an xCorner or yCenter in the ascii file</param>
    private void GenerateLatLonValues(double xllCorner, double yllCorner, double cellSize, bool isBllX, bool isBllY)
    {
      List<double> xScale = new List<double>();
      List<double> yScale = new List<double>();
      double scale = xllCorner;
      if (isBllX)
      {
        for (int j = 0; j < header.NumX; j++)
        {
          xScale.Add(scale);
          scale += cellSize;
        }
      }
      else
      {
        //TODO: handle if XCenter
      }

      scale = yllCorner;
      if (isBllY)
      {
        for (int j = 0; j < header.NumY; j++)
        {
          yScale.Add(scale);
          scale += cellSize;
        }
      }
      else
      {
        //TODO: handle if XCenter
      }
      // ASCII Grid starts at the lower left corner, DataGrid assumes the data starts at the upper left
      // So we flip the Y order here so that it gets rendered correctly
      yScale.Reverse();

      this.DataGrid = new DataGrid(yScale.ToArray(), xScale.ToArray(), new DataGrid.Bounds(header.MinX, header.MaxX, header.MinY, header.MaxY));
    }

    /// <summary>
    /// Reads ASCII Grid header
    /// </summary>
    /// <param name="streamReader"></param>
    /// <returns></returns>
    protected override bool ReadDimensions(StreamReader streamReader)
    {
      bool headerRead = false;

      int noX = 0, noY = 0;
      double noDataVal = -9999;
      double blX = 0, blY = 0, cellSize = 0;

      bool isBLLx = true;
      bool isBLLy = true;

      string line;
      while ((line = streamReader.ReadLine()) != null)
      {
        var tokens = line.Split(new char[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 2)
        {
          switch (tokens[0])
          {
            case "ncols":
              {
                noX = Convert.ToInt32(tokens[1]); break;
              }
            case "nrows":
              {
                noY = Convert.ToInt32(tokens[1]);
                break;
              }
            case "xllcenter":
              {
                isBLLx = false;
                goto case "xllcorner";
              }
            case "xllcorner":
              {
                blX = Convert.ToDouble(tokens[1]);
                break;
              }
            case "yllcenter":
              {
                isBLLy = false;
                goto case "yllcorner";
              }
            case "yllcorner":
              {
                blY = Convert.ToDouble(tokens[1]);
                break;
              }
            case "cellsize":
              {
                cellSize = Convert.ToDouble(tokens[1]);
                break;
              }
            case "NODATA_value":
              {
                noDataVal = Convert.ToDouble(tokens[1]);
                headerRead = true; // This is the last line of the header
                break;
              }
          }
        }
        if (headerRead) break;
      }

      populateHeader(noX, noY, blX, blY, noDataVal, cellSize, isBLLx, isBLLy);

      GenerateLatLonValues(blX, blY, cellSize, isBLLx, isBLLy);

      return headerRead;
    }

    /// <summary>
    /// Read z values and stores them in the grid
    /// </summary>
    /// <param name="streamReader">ASCII grid stream (it points to the right location where z values start in the ASCII grid file)</param>
    /// <returns></returns>
    protected override bool ReadDataGrid(StreamReader streamReader)
    {
      bool readSome = false;
      string line;
      int y = 0;

      while ((line = streamReader.ReadLine()) != null)
      {
        var tokens = line.Split(Delimiter);
        int x = 0;
        foreach (var token in tokens)
        {
          if (x > 0)
          {
            // Read a z value
            var z = token.Count() == 0 ? header.NullVal : Convert.ToDouble(token);

            // Store only genuine values and ignore the ones where there is no data
            if (z != header.NullVal)
            {
              DataGrid.SetValue(x - 1, y, z);
            }

            readSome = true;
          }

          ++x;
        }

        ++y;
      }

      return readSome;
    }
  }

}