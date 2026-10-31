using System.Globalization;

namespace WheelConvert.Core.Services;

public readonly record struct PointD(double X, double Y);

/// <summary>
/// Pure math for the wheel. Angles are in degrees, measured CLOCKWISE from 12 o'clock.
/// Segment 0 is centred at the top; segments run clockwise. Screen coordinates (Y grows downward).
/// </summary>
public static class RadialGeometry
{
    public static double SegmentWidth(int count) => 360.0 / count;

    public static double NormalizeAngle(double angle)
    {
        angle %= 360.0;
        return angle < 0 ? angle + 360.0 : angle;
    }

    /// <summary>Angle of an offset (dx, dy) from the wheel centre, clockwise from 12 o'clock, 0..360.</summary>
    public static double AngleOf(double dx, double dy) => NormalizeAngle(Math.Atan2(dx, -dy) * 180.0 / Math.PI);

    public static double SegmentCenterAngle(int index, int count) => index * SegmentWidth(count);

    public static PointD PointAt(double cx, double cy, double radius, double angleDegrees)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        return new PointD(cx + radius * Math.Sin(rad), cy - radius * Math.Cos(rad));
    }

    /// <summary>Segment index under offset (dx, dy), or -1 when inside the centre hole or outside the ring.</summary>
    public static int HitTest(double dx, double dy, double innerRadius, double outerRadius, int count)
    {
        if (count <= 0) return -1;
        var r = Math.Sqrt(dx * dx + dy * dy);
        if (r < innerRadius || r > outerRadius) return -1;

        var width = SegmentWidth(count);
        var shifted = NormalizeAngle(AngleOf(dx, dy) + width / 2.0); // segment 0 straddles 12 o'clock
        var index = (int)Math.Floor(shifted / width);
        return index >= count ? 0 : index;
    }

    public static bool IsInCenter(double dx, double dy, double innerRadius) => Math.Sqrt(dx * dx + dy * dy) < innerRadius;

    /// <summary>
    /// WPF path-mini-language string for one ring segment, with an angular gap between neighbours.
    /// Feed to Geometry.Parse(). Uses invariant culture so it never breaks on comma-decimal locales.
    /// </summary>
    public static string SegmentPath(double cx, double cy, double innerRadius, double outerRadius,
                                     int index, int count, double gapDegrees)
    {
        var width = SegmentWidth(count);
        var center = SegmentCenterAngle(index, count);
        var start = center - width / 2.0 + gapDegrees / 2.0;
        var end = center + width / 2.0 - gapDegrees / 2.0;
        var large = (end - start) > 180.0 ? 1 : 0;

        var p1 = PointAt(cx, cy, outerRadius, start);
        var p2 = PointAt(cx, cy, outerRadius, end);
        var p3 = PointAt(cx, cy, innerRadius, end);
        var p4 = PointAt(cx, cy, innerRadius, start);

        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        return $"M {F(p1.X)},{F(p1.Y)} " +
               $"A {F(outerRadius)},{F(outerRadius)} 0 {large} 1 {F(p2.X)},{F(p2.Y)} " +
               $"L {F(p3.X)},{F(p3.Y)} " +
               $"A {F(innerRadius)},{F(innerRadius)} 0 {large} 0 {F(p4.X)},{F(p4.Y)} Z";
    }
}
