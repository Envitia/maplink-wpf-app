using System.Windows;

namespace Envitia.MapLink.Grids
{
  /// <summary>
  /// Represent a straight line between two given Co-ordinates
  /// </summary>
  internal class Line
  {
    public Point p1, p2;

    public Line(Point p1, Point p2)
    {
      this.p1 = p1;
      this.p2 = p2;
    }

    /// <summary>
    /// Returns a set of equally spaced Points within the line
    /// </summary>
    /// <param name="quantity">Number of Points to return</param>
    /// <returns></returns>
    public Point[] getPoints(int quantity)
    {
      var points = new Point[quantity];
      double ydiff = p2.Y - p1.Y, xdiff = p2.X - p1.X;
      double slope = (double)(p2.Y - p1.Y) / (p2.X - p1.X);
      double x, y;

      --quantity;

      for (double i = 0; i < quantity; i++)
      {
        y = slope == 0 ? 0 : ydiff * (i / quantity);
        x = slope == 0 ? xdiff * (i / quantity) : y / slope;
        points[(int)i] = new Point(x + p1.X, y + p1.Y);
      }

      points[quantity] = p2;
      return points;
    }
  }
}
