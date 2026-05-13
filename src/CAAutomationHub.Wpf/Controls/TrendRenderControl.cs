using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Controls;

public sealed class TrendRenderControl : FrameworkElement
{
    private const double TrendWindowMinutes = 30;
    private const double MinimumFixedMaxY = 1000;
    private const int WorstPriority = 100;

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<TrendPoint>),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(Array.Empty<TrendPoint>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series),
        typeof(IReadOnlyList<CommunicationTrendSeries>),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(Array.Empty<CommunicationTrendSeries>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WarningThresholdMsProperty = DependencyProperty.Register(
        nameof(WarningThresholdMs),
        typeof(double),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CongestedThresholdMsProperty = DependencyProperty.Register(
        nameof(CongestedThresholdMs),
        typeof(double),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ErrorThresholdMsProperty = DependencyProperty.Register(
        nameof(ErrorThresholdMs),
        typeof(double),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsOverviewProperty = DependencyProperty.Register(
        nameof(IsOverview),
        typeof(bool),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowThresholdsProperty = DependencyProperty.Register(
        nameof(ShowThresholds),
        typeof(bool),
        typeof(TrendRenderControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<TrendPoint> Points
    {
        get => (IReadOnlyList<TrendPoint>)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IReadOnlyList<CommunicationTrendSeries> Series
    {
        get => (IReadOnlyList<CommunicationTrendSeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public double WarningThresholdMs
    {
        get => (double)GetValue(WarningThresholdMsProperty);
        set => SetValue(WarningThresholdMsProperty, value);
    }

    public double CongestedThresholdMs
    {
        get => (double)GetValue(CongestedThresholdMsProperty);
        set => SetValue(CongestedThresholdMsProperty, value);
    }

    public double ErrorThresholdMs
    {
        get => (double)GetValue(ErrorThresholdMsProperty);
        set => SetValue(ErrorThresholdMsProperty, value);
    }

    public bool IsOverview
    {
        get => (bool)GetValue(IsOverviewProperty);
        set => SetValue(IsOverviewProperty, value);
    }

    public bool ShowThresholds
    {
        get => (bool)GetValue(ShowThresholdsProperty);
        set => SetValue(ShowThresholdsProperty, value);
    }

    public static int GetSeriesRenderPriority(PlcConnectionState state, bool isWorst)
    {
        if (isWorst) return WorstPriority;

        return state switch
        {
            PlcConnectionState.Inactive => 0,
            PlcConnectionState.Healthy => 1,
            PlcConnectionState.Warning => 2,
            PlcConnectionState.Congested => 3,
            PlcConnectionState.Error => 4,
            _ => 1
        };
    }

    public static PlcConnectionState GetRttSegmentState(
        double responseMs,
        double warningThresholdMs,
        double congestedThresholdMs,
        double errorThresholdMs)
    {
        if (responseMs >= errorThresholdMs) return PlcConnectionState.Error;
        if (responseMs >= congestedThresholdMs) return PlcConnectionState.Congested;
        if (responseMs >= warningThresholdMs) return PlcConnectionState.Warning;
        return PlcConnectionState.Healthy;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var plot = new Rect(54, 12, Math.Max(0, bounds.Width - 66), Math.Max(0, bounds.Height - 34));
        DrawBackground(dc, bounds, plot);
        DrawAxisLabels(dc, bounds, plot);

        var points = Points ?? Array.Empty<TrendPoint>();
        var series = Series ?? Array.Empty<CommunicationTrendSeries>();
        if (IsOverview && series.Count > 0 && plot.Width > 0 && plot.Height > 0)
        {
            DrawOverviewSeries(dc, plot, series, ShowThresholds);
            return;
        }

        if (points.Count == 0 || plot.Width <= 0 || plot.Height <= 0)
        {
            DrawEmptyState(dc, bounds);
            return;
        }

        var yMax = GetFixedMaxY();

        if (ShowThresholds)
        {
            DrawThresholdLine(dc, plot, yMax, WarningThresholdMs, "Warn", new Pen(new SolidColorBrush(Color.FromArgb(150, 240, 220, 120)), 1));
            DrawThresholdLine(dc, plot, yMax, CongestedThresholdMs, "Congested", new Pen(new SolidColorBrush(Color.FromArgb(145, 255, 151, 45)), 1));
            DrawThresholdLine(dc, plot, yMax, ErrorThresholdMs, "Error", new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 99, 99)), 1));
        }

        DrawSegmentedLine(dc, plot, points, yMax, isWorst: false, isOverview: false);
    }

    private void DrawOverviewSeries(DrawingContext dc, Rect plot, IReadOnlyList<CommunicationTrendSeries> series, bool showThresholds)
    {
        var yMax = GetFixedMaxY();

        foreach (var item in series.OrderBy(item => GetSeriesRenderPriority(item.State, isWorst: false)))
        {
            if (item.Points.Count == 0) continue;

            if (item.State == PlcConnectionState.Inactive)
            {
                DrawSeriesLine(dc, plot, item.Points, yMax, CreateInactiveSeriesPen());
                continue;
            }

            DrawSegmentedLine(dc, plot, item.Points, yMax, item.IsWorst, isOverview: true);
        }

        if (showThresholds)
        {
            DrawThresholdLine(dc, plot, yMax, WarningThresholdMs, "Warn", new Pen(new SolidColorBrush(Color.FromArgb(95, 240, 220, 120)), 0.8));
            DrawThresholdLine(dc, plot, yMax, CongestedThresholdMs, "Congested", new Pen(new SolidColorBrush(Color.FromArgb(95, 255, 151, 45)), 0.8));
            DrawThresholdLine(dc, plot, yMax, ErrorThresholdMs, "Error", new Pen(new SolidColorBrush(Color.FromArgb(115, 255, 99, 99)), 0.8));
        }
    }

    private Pen CreateSegmentPen(PlcConnectionState segmentState, bool isWorst, bool isOverview)
    {
        var (color, thickness, opacity) = segmentState switch
        {
            PlcConnectionState.Healthy => (Color.FromRgb(80, 178, 255), 0.9, 0.30),
            PlcConnectionState.Warning => (Color.FromRgb(255, 220, 86), 1.35, 0.68),
            PlcConnectionState.Congested => (Color.FromRgb(255, 151, 45), 1.85, 0.86),
            PlcConnectionState.Error => (Color.FromRgb(255, 82, 82), 2.3, 0.98),
            _ => (Color.FromRgb(159, 174, 199), 1.0, 0.40)
        };

        if (!isOverview) opacity = Math.Min(1.0, opacity + 0.12);
        if (isWorst)
        {
            thickness += 0.9;
            opacity = Math.Min(1.0, opacity + 0.18);
        }

        var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B)), thickness);
        if (segmentState == PlcConnectionState.Congested) pen.DashStyle = DashStyles.DashDot;

        return pen;
    }

    private static Pen CreateInactiveSeriesPen()
        => new(new SolidColorBrush(Color.FromArgb(40, 130, 143, 161)), 0.8)
        {
            DashStyle = DashStyles.Dot
        };

    private static void DrawSeriesLine(DrawingContext dc, Rect plot, IReadOnlyList<TrendPoint> points, double yMax, Pen pen)
    {
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(ToPoint(plot, points[0], 0, points.Count, yMax), isFilled: false, isClosed: false);

            for (var index = 1; index < points.Count; index++)
            {
                context.LineTo(ToPoint(plot, points[index], index, points.Count, yMax), isStroked: true, isSmoothJoin: false);
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    private void DrawSegmentedLine(DrawingContext dc, Rect plot, IReadOnlyList<TrendPoint> points, double yMax, bool isWorst, bool isOverview)
    {
        if (points.Count == 1)
        {
            var state = GetRttSegmentState(points[0].ResponseMs, WarningThresholdMs, CongestedThresholdMs, ErrorThresholdMs);
            var center = ToPoint(plot, points[0], 0, points.Count, yMax);
            var pen = CreateSegmentPen(state, isWorst, isOverview);
            dc.DrawEllipse(pen.Brush, null, center, pen.Thickness, pen.Thickness);
            return;
        }

        for (var index = 1; index < points.Count; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            var segmentResponseMs = Math.Max(previous.ResponseMs, current.ResponseMs);
            var state = GetRttSegmentState(segmentResponseMs, WarningThresholdMs, CongestedThresholdMs, ErrorThresholdMs);
            var pen = CreateSegmentPen(state, isWorst, isOverview);

            dc.DrawLine(
                pen,
                ToPoint(plot, previous, index - 1, points.Count, yMax),
                ToPoint(plot, current, index, points.Count, yMax));
        }
    }

    private static void DrawWorstSeriesHighlight(DrawingContext dc, Rect plot, IReadOnlyList<TrendPoint> points, double yMax)
    {
        var haloPen = new Pen(new SolidColorBrush(Color.FromArgb(165, 255, 255, 255)), 3.8)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        var corePen = new Pen(new SolidColorBrush(Color.FromArgb(235, 48, 197, 255)), 2.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        DrawSeriesLine(dc, plot, points, yMax, haloPen);
        DrawSeriesLine(dc, plot, points, yMax, corePen);
    }

    private static void DrawOverviewErrorMarkers(DrawingContext dc, Rect plot, IReadOnlyList<CommunicationTrendSeries> series, double yMax)
    {
        var candidates = series
            .SelectMany(item => item.Points.Select((point, index) => new { item, point, index }))
            .Where(item => item.point.MarkerKind == TrendMarkerKind.Error)
            .ToArray();

        if (candidates.Length == 0) return;

        const int maxMarkers = 36;
        var stride = Math.Max(1, (int)Math.Ceiling(candidates.Length / (double)maxMarkers));
        var brush = new SolidColorBrush(Color.FromArgb(210, 255, 99, 99));
        var border = new Pen(new SolidColorBrush(Color.FromRgb(18, 24, 35)), 0.8);

        for (var i = 0; i < candidates.Length; i += stride)
        {
            var candidate = candidates[i];
            var center = ToPoint(plot, candidate.point, candidate.index, candidate.item.Points.Count, yMax);
            dc.DrawEllipse(brush, border, center, 3.2, 3.2);
        }
    }

    private static void DrawBackground(DrawingContext dc, Rect bounds, Rect plot)
    {
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(22, 30, 44)), null, bounds);

        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 67, 84, 115)), 0.5);
        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + (plot.Height / 4 * i);
            dc.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
        }

        for (var i = 0; i <= 6; i++)
        {
            var x = plot.Left + (plot.Width / 6 * i);
            dc.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }
    }

    private void DrawAxisLabels(DrawingContext dc, Rect bounds, Rect plot)
    {
        var labelBrush = new SolidColorBrush(Color.FromRgb(159, 174, 199));
        DrawText(dc, "RTT(ms)", new Point(6, plot.Top), labelBrush, 10);
        DrawText(dc, $"{TrendWindowMinutes:0}분 전", new Point(plot.Left, plot.Bottom + 5), labelBrush, 10);
        DrawText(dc, "시간", new Point(plot.Left + (plot.Width / 2) - 10, plot.Bottom + 5), labelBrush, 10);

        var nowText = CreateText("현재", labelBrush, 10);
        dc.DrawText(nowText, new Point(Math.Min(plot.Right - nowText.Width, bounds.Right - nowText.Width - 4), plot.Bottom + 5));
    }

    private void DrawEmptyState(DrawingContext dc, Rect bounds)
    {
        var text = new FormattedText(
            "No trend data",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            new SolidColorBrush(Color.FromRgb(159, 174, 199)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawText(text, new Point((bounds.Width - text.Width) / 2, (bounds.Height - text.Height) / 2));
    }

    private void DrawThresholdLine(DrawingContext dc, Rect plot, double yMax, double thresholdMs, string label, Pen pen)
    {
        if (thresholdMs <= 0 || thresholdMs > yMax) return;

        var y = ToY(plot, thresholdMs, yMax);
        pen.DashStyle = DashStyles.Dash;
        dc.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));

        var text = CreateText($"{label} {thresholdMs:0}ms", pen.Brush, 10);

        dc.DrawText(text, new Point(plot.Left + 4, Math.Max(plot.Top, y - text.Height - 2)));
    }

    private void DrawText(DrawingContext dc, string text, Point origin, Brush brush, double fontSize)
        => dc.DrawText(CreateText(text, brush, fontSize), origin);

    private FormattedText CreateText(string text, Brush brush, double fontSize)
        => new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSize,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private static void DrawResponseLine(DrawingContext dc, Rect plot, IReadOnlyList<TrendPoint> points, double yMax)
    {
        var linePen = new Pen(new SolidColorBrush(Color.FromRgb(48, 197, 255)), 1.8);
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(ToPoint(plot, points[0], 0, points.Count, yMax), isFilled: false, isClosed: false);

            for (var index = 1; index < points.Count; index++)
            {
                context.LineTo(ToPoint(plot, points[index], index, points.Count, yMax), isStroked: true, isSmoothJoin: false);
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(null, linePen, geometry);
    }

    private static void DrawMarkers(DrawingContext dc, Rect plot, IReadOnlyList<TrendPoint> points, double yMax, bool isOverview)
    {
        var warningBrush = new SolidColorBrush(Color.FromRgb(240, 220, 120));
        var errorBrush = new SolidColorBrush(Color.FromRgb(255, 99, 99));
        var markerBorder = new Pen(new SolidColorBrush(Color.FromRgb(18, 24, 35)), 1);
        var markerIndexes = GetMarkerIndexes(points, isOverview).ToHashSet();

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            if (point.MarkerKind == TrendMarkerKind.None) continue;
            if (!markerIndexes.Contains(index)) continue;

            var center = ToPoint(plot, point, index, points.Count, yMax);
            var radius = point.MarkerKind == TrendMarkerKind.Error ? 4.2 : 3.2;
            var brush = point.MarkerKind == TrendMarkerKind.Error ? errorBrush : warningBrush;
            dc.DrawEllipse(brush, markerBorder, center, radius, radius);
        }
    }

    private static IEnumerable<int> GetMarkerIndexes(IReadOnlyList<TrendPoint> points, bool isOverview)
    {
        if (!isOverview)
        {
            for (var index = 0; index < points.Count; index++)
            {
                if (points[index].MarkerKind != TrendMarkerKind.None) yield return index;
            }

            yield break;
        }

        var errorIndexes = points
            .Select((point, index) => new { point, index })
            .Where(item => item.point.MarkerKind == TrendMarkerKind.Error)
            .Select(item => item.index)
            .ToArray();
        const int maxOverviewErrorMarkers = 48;

        if (errorIndexes.Length <= maxOverviewErrorMarkers)
        {
            foreach (var index in errorIndexes) yield return index;
            yield break;
        }

        var stride = Math.Ceiling(errorIndexes.Length / (double)maxOverviewErrorMarkers);
        for (var index = 0; index < errorIndexes.Length; index++)
        {
            if (index % stride == 0) yield return errorIndexes[index];
        }
    }

    private double GetFixedMaxY()
        => Math.Max(Math.Max(ErrorThresholdMs * 1.2, Math.Max(CongestedThresholdMs, WarningThresholdMs) * 1.2), MinimumFixedMaxY);

    private static Point ToPoint(Rect plot, TrendPoint point, int index, int count, double yMax)
        => new(ToX(plot, index, count), ToY(plot, point.ResponseMs, yMax));

    private static double ToX(Rect plot, int index, int count)
        => count <= 1 ? plot.Left : plot.Left + (plot.Width * index / (count - 1));

    private static double ToY(Rect plot, double responseMs, double yMax)
    {
        var ratio = yMax <= 0 ? 0 : Math.Clamp(responseMs / yMax, 0, 1);
        return plot.Bottom - (plot.Height * ratio);
    }
}
