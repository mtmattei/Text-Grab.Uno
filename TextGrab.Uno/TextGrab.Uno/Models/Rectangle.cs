namespace TextGrab.Models;

/// <summary>
/// Simple integer Rectangle struct replacing System.Drawing.Rectangle for cross-platform use.
/// Used by ResultTable for table analysis geometry.
/// </summary>
public struct Rectangle
{
    public int X, Y, Width, Height;
}
