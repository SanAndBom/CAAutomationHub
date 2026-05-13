using System.Windows;
using System.Windows.Controls;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Controls;

public partial class CommunicationTrendChart : UserControl
{
    public static readonly DependencyProperty TrendProperty = DependencyProperty.Register(
        nameof(Trend),
        typeof(CommunicationTrendSnapshot),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(CommunicationTrendSnapshot.Empty, OnTrendChanged));

    public static readonly DependencyProperty AverageResponseMsProperty = DependencyProperty.Register(
        nameof(AverageResponseMs),
        typeof(double),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(0d, OnSummaryMetricChanged));

    public static readonly DependencyProperty MaxResponseMsProperty = DependencyProperty.Register(
        nameof(MaxResponseMs),
        typeof(double),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(0d, OnSummaryMetricChanged));

    public static readonly DependencyProperty ErrorCountProperty = DependencyProperty.Register(
        nameof(ErrorCount),
        typeof(int),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(0, OnSummaryMetricChanged));

    public static readonly DependencyProperty PointCountProperty = DependencyProperty.Register(
        nameof(PointCount),
        typeof(int),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(0, OnSummaryMetricChanged));

    public static readonly DependencyProperty AverageLabelProperty = DependencyProperty.Register(
        nameof(AverageLabel),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("Avg Response"));

    public static readonly DependencyProperty MaxLabelProperty = DependencyProperty.Register(
        nameof(MaxLabel),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("Peak Response"));

    public static readonly DependencyProperty ErrorLabelProperty = DependencyProperty.Register(
        nameof(ErrorLabel),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("Error Points"));

    public static readonly DependencyProperty PointLabelProperty = DependencyProperty.Register(
        nameof(PointLabel),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("Samples"));

    public static readonly DependencyProperty SubtitleTextProperty = DependencyProperty.Register(
        nameof(SubtitleText),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CriteriaTextProperty = DependencyProperty.Register(
        nameof(CriteriaText),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty WorstSummaryTextProperty = DependencyProperty.Register(
        nameof(WorstSummaryText),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowThresholdsProperty = DependencyProperty.Register(
        nameof(ShowThresholds),
        typeof(bool),
        typeof(CommunicationTrendChart),
        new PropertyMetadata(true));

    public static readonly DependencyProperty SummaryLabel1Property = DependencyProperty.Register(
        nameof(SummaryLabel1),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("평균 응답"));

    public static readonly DependencyProperty SummaryValue1Property = DependencyProperty.Register(
        nameof(SummaryValue1),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("0 ms"));

    public static readonly DependencyProperty SummaryLabel2Property = DependencyProperty.Register(
        nameof(SummaryLabel2),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("최대 응답"));

    public static readonly DependencyProperty SummaryValue2Property = DependencyProperty.Register(
        nameof(SummaryValue2),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("0 ms"));

    public static readonly DependencyProperty SummaryLabel3Property = DependencyProperty.Register(
        nameof(SummaryLabel3),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("오류 샘플"));

    public static readonly DependencyProperty SummaryValue3Property = DependencyProperty.Register(
        nameof(SummaryValue3),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("0"));

    public static readonly DependencyProperty SummaryLabel4Property = DependencyProperty.Register(
        nameof(SummaryLabel4),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("샘플 수"));

    public static readonly DependencyProperty SummaryValue4Property = DependencyProperty.Register(
        nameof(SummaryValue4),
        typeof(string),
        typeof(CommunicationTrendChart),
        new PropertyMetadata("0"));

    public CommunicationTrendChart() => InitializeComponent();

    public CommunicationTrendSnapshot Trend
    {
        get => (CommunicationTrendSnapshot)GetValue(TrendProperty);
        set => SetValue(TrendProperty, value);
    }

    public double AverageResponseMs
    {
        get => (double)GetValue(AverageResponseMsProperty);
        set => SetValue(AverageResponseMsProperty, value);
    }

    public double MaxResponseMs
    {
        get => (double)GetValue(MaxResponseMsProperty);
        set => SetValue(MaxResponseMsProperty, value);
    }

    public int ErrorCount
    {
        get => (int)GetValue(ErrorCountProperty);
        set => SetValue(ErrorCountProperty, value);
    }

    public int PointCount
    {
        get => (int)GetValue(PointCountProperty);
        set => SetValue(PointCountProperty, value);
    }

    public string AverageLabel
    {
        get => (string)GetValue(AverageLabelProperty);
        private set => SetValue(AverageLabelProperty, value);
    }

    public string MaxLabel
    {
        get => (string)GetValue(MaxLabelProperty);
        private set => SetValue(MaxLabelProperty, value);
    }

    public string ErrorLabel
    {
        get => (string)GetValue(ErrorLabelProperty);
        private set => SetValue(ErrorLabelProperty, value);
    }

    public string PointLabel
    {
        get => (string)GetValue(PointLabelProperty);
        private set => SetValue(PointLabelProperty, value);
    }

    public string SubtitleText
    {
        get => (string)GetValue(SubtitleTextProperty);
        private set => SetValue(SubtitleTextProperty, value);
    }

    public string CriteriaText
    {
        get => (string)GetValue(CriteriaTextProperty);
        private set => SetValue(CriteriaTextProperty, value);
    }

    public string WorstSummaryText
    {
        get => (string)GetValue(WorstSummaryTextProperty);
        private set => SetValue(WorstSummaryTextProperty, value);
    }

    public bool ShowThresholds
    {
        get => (bool)GetValue(ShowThresholdsProperty);
        private set => SetValue(ShowThresholdsProperty, value);
    }

    public string SummaryLabel1
    {
        get => (string)GetValue(SummaryLabel1Property);
        private set => SetValue(SummaryLabel1Property, value);
    }

    public string SummaryValue1
    {
        get => (string)GetValue(SummaryValue1Property);
        private set => SetValue(SummaryValue1Property, value);
    }

    public string SummaryLabel2
    {
        get => (string)GetValue(SummaryLabel2Property);
        private set => SetValue(SummaryLabel2Property, value);
    }

    public string SummaryValue2
    {
        get => (string)GetValue(SummaryValue2Property);
        private set => SetValue(SummaryValue2Property, value);
    }

    public string SummaryLabel3
    {
        get => (string)GetValue(SummaryLabel3Property);
        private set => SetValue(SummaryLabel3Property, value);
    }

    public string SummaryValue3
    {
        get => (string)GetValue(SummaryValue3Property);
        private set => SetValue(SummaryValue3Property, value);
    }

    public string SummaryLabel4
    {
        get => (string)GetValue(SummaryLabel4Property);
        private set => SetValue(SummaryLabel4Property, value);
    }

    public string SummaryValue4
    {
        get => (string)GetValue(SummaryValue4Property);
        private set => SetValue(SummaryValue4Property, value);
    }

    private static void OnSummaryMetricChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        => ((CommunicationTrendChart)dependencyObject).UpdateSummaryText();

    private static void OnTrendChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var chart = (CommunicationTrendChart)dependencyObject;
        var trend = e.NewValue as CommunicationTrendSnapshot ?? CommunicationTrendSnapshot.Empty;

        if (trend.IsOverview)
        {
            chart.SubtitleText = "기준: 동일 시간축 PLC별 RTT 비교";
            chart.CriteriaText = CreateCriteriaText(trend, includeColorMeaning: true);
            chart.WorstSummaryText = CreateWorstSummaryText(trend);
            chart.ShowThresholds = true;
            chart.UpdateSummaryText();
            return;
        }

        chart.SubtitleText = "기준: 선택 PLC RTT";
        chart.CriteriaText = CreateCriteriaText(trend, includeColorMeaning: false);
        chart.WorstSummaryText = string.Empty;
        chart.ShowThresholds = true;
        chart.UpdateSummaryText();
    }

    private static string CreateWorstSummaryText(CommunicationTrendSnapshot trend)
        => string.IsNullOrWhiteSpace(trend.WorstPlcName) || trend.WorstResponseMs is null
            ? string.Empty
            : $"Worst: {trend.WorstPlcName} / Peak {trend.WorstResponseMs:0}ms";

    private static string CreateCriteriaText(CommunicationTrendSnapshot trend, bool includeColorMeaning)
    {
        var stateCriteria = $"RTT 기준: 정상 <{trend.WarningThresholdMs:0} · 주의 {trend.WarningThresholdMs:0}~{trend.CongestedThresholdMs:0} · 정체 {trend.CongestedThresholdMs:0}~{trend.ErrorThresholdMs:0} · 오류 >={trend.ErrorThresholdMs:0}/실패";
        return $"{stateCriteria} · 색상=RTT 구간, 높이=RTT";
    }

    private void UpdateSummaryText()
    {
        if (Trend.IsOverview)
        {
            var healthyCount = Trend.Series.Count(series => series.State == PlcConnectionState.Healthy);
            var warningOrCongestedCount = Trend.Series.Count(series => series.State is PlcConnectionState.Warning or PlcConnectionState.Congested);
            var errorCount = Trend.Series.Count(series => series.State == PlcConnectionState.Error);
            var inactiveCount = Trend.Series.Count(series => series.State == PlcConnectionState.Inactive);

            SummaryLabel1 = "정상";
            SummaryValue1 = healthyCount.ToString();
            SummaryLabel2 = "주의·정체";
            SummaryValue2 = warningOrCongestedCount.ToString();
            SummaryLabel3 = "오류 PLC";
            SummaryValue3 = errorCount.ToString();
            SummaryLabel4 = "비활성";
            SummaryValue4 = inactiveCount.ToString();
            return;
        }

        SummaryLabel1 = "평균 RTT";
        SummaryValue1 = $"{AverageResponseMs:0} ms";
        SummaryLabel2 = "최대 RTT";
        SummaryValue2 = $"{MaxResponseMs:0} ms";
        SummaryLabel3 = "오류 샘플";
        SummaryValue3 = ErrorCount.ToString();
        SummaryLabel4 = "샘플 수";
        SummaryValue4 = PointCount.ToString();
    }
}
