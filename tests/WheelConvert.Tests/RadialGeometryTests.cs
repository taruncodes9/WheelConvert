using WheelConvert.Core.Services;
using Xunit;

namespace WheelConvert.Tests;

public class RadialGeometryTests
{
    private const double Inner = 50, Outer = 150, Mid = 100;

    public static IEnumerable<object[]> Counts() => Enumerable.Range(2, 11).Select(n => new object[] { n });

    [Theory, MemberData(nameof(Counts))]
    public void SegmentCenter_HitsItsOwnIndex(int n)
    {
        for (var i = 0; i < n; i++)
        {
            var p = RadialGeometry.PointAt(0, 0, Mid, RadialGeometry.SegmentCenterAngle(i, n));
            Assert.Equal(i, RadialGeometry.HitTest(p.X, p.Y, Inner, Outer, n));
        }
    }

    [Theory, MemberData(nameof(Counts))]
    public void NearSegmentEdges_StillHitSameIndex(int n)
    {
        var w = RadialGeometry.SegmentWidth(n);
        for (var i = 0; i < n; i++)
        foreach (var sign in new[] { -1, 1 })
        {
            var angle = RadialGeometry.SegmentCenterAngle(i, n) + sign * w * 0.45;
            var p = RadialGeometry.PointAt(0, 0, Mid, angle);
            Assert.Equal(i, RadialGeometry.HitTest(p.X, p.Y, Inner, Outer, n));
        }
    }

    [Theory, MemberData(nameof(Counts))]
    public void JustPastTheEdge_HitsNeighbour(int n)
    {
        var w = RadialGeometry.SegmentWidth(n);
        var p = RadialGeometry.PointAt(0, 0, Mid, RadialGeometry.SegmentCenterAngle(0, n) + w * 0.55);
        Assert.Equal(1 % n, RadialGeometry.HitTest(p.X, p.Y, Inner, Outer, n));
    }

    [Fact]
    public void InsideCenterHole_And_OutsideRing_ReturnMinusOne()
    {
        Assert.Equal(-1, RadialGeometry.HitTest(0, 0, Inner, Outer, 8));
        Assert.Equal(-1, RadialGeometry.HitTest(10, 10, Inner, Outer, 8));
        Assert.Equal(-1, RadialGeometry.HitTest(0, -200, Inner, Outer, 8));
        Assert.True(RadialGeometry.IsInCenter(10, 10, Inner));
    }

    [Fact]
    public void Cardinal_Directions_With4Segments()
    {
        Assert.Equal(0, RadialGeometry.HitTest(0, -100, Inner, Outer, 4));    // top
        Assert.Equal(1, RadialGeometry.HitTest(100, 0, Inner, Outer, 4));     // right
        Assert.Equal(2, RadialGeometry.HitTest(0, 100, Inner, Outer, 4));     // bottom
        Assert.Equal(3, RadialGeometry.HitTest(-100, 0, Inner, Outer, 4));    // left
        Assert.Equal(0, RadialGeometry.HitTest(-1, -100, Inner, Outer, 8));   // just left of top still segment 0
    }

    [Fact]
    public void ZeroCount_ReturnsMinusOne() => Assert.Equal(-1, RadialGeometry.HitTest(0, -100, Inner, Outer, 0));

    [Fact]
    public void PointAt_Angles()
    {
        var top = RadialGeometry.PointAt(10, 10, 5, 0);
        Assert.Equal(10, top.X, 6); Assert.Equal(5, top.Y, 6);
        var right = RadialGeometry.PointAt(10, 10, 5, 90);
        Assert.Equal(15, right.X, 6); Assert.Equal(10, right.Y, 6);
    }

    [Theory, MemberData(nameof(Counts))]
    public void SegmentPath_IsWellFormed(int n)
    {
        var path = RadialGeometry.SegmentPath(200, 200, 60, 170, 0, n, 3);
        Assert.StartsWith("M ", path);
        Assert.EndsWith("Z", path);
        Assert.DoesNotContain("NaN", path);
        Assert.Equal(2, path.Split('A').Length - 1);   // exactly two arcs
    }
}
