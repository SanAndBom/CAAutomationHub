namespace CAAutomationHub.PilotSmoke;

public static class PilotSmokeConfigurationLoader
{
    private const string ExecuteReadOnlyFlag = "--execute-read-only";

    public static PilotSmokeConfiguration Load(
        string[] args,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var values = ParseArgs(args);
        var executeRequested = values.ContainsKey(ExecuteReadOnlyFlag)
            || string.Equals(
                getEnvironmentVariable("CAAH_PILOT_PLC_EXECUTE_READ_ONLY"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        var host = GetValue(values, "--host", getEnvironmentVariable, "CAAH_PILOT_PLC_HOST");
        var portText = GetValue(values, "--port", getEnvironmentVariable, "CAAH_PILOT_PLC_PORT");
        var readStart = GetValue(values, "--read-start", getEnvironmentVariable, "CAAH_PILOT_PLC_READ_START_VARIABLE");
        var readWordCountText = GetValue(values, "--read-word-count", getEnvironmentVariable, "CAAH_PILOT_PLC_READ_WORD_COUNT");
        var startSignalWordIndexText = GetValue(
            values,
            "--start-signal-word-index",
            getEnvironmentVariable,
            "CAAH_PILOT_PLC_START_SIGNAL_WORD_INDEX");
        var lotId1WordOffsetText = GetValue(
            values,
            "--lot-id1-word-offset",
            getEnvironmentVariable,
            "CAAH_PILOT_PLC_LOT_ID1_WORD_OFFSET");
        var lotId2WordOffsetText = GetValue(
            values,
            "--lot-id2-word-offset",
            getEnvironmentVariable,
            "CAAH_PILOT_PLC_LOT_ID2_WORD_OFFSET");
        var lotIdWordLengthText = GetValue(
            values,
            "--lot-id-word-length",
            getEnvironmentVariable,
            "CAAH_PILOT_PLC_LOT_ID_WORD_LENGTH");

        var connection = new PilotSmokeConnectionOptions(
            host ?? string.Empty,
            ParseIntOrZero(portText),
            ConnectTimeout: TimeSpan.FromSeconds(5),
            SendTimeout: TimeSpan.FromSeconds(5),
            ReceiveTimeout: TimeSpan.FromSeconds(5));
        var read = new PilotSmokeReadOptions(
            readStart ?? string.Empty,
            ParseIntOrZero(readWordCountText));
        var layout = new PilotSmokeReadLayout(
            ParseIntOrZero(startSignalWordIndexText),
            ParseIntOrZero(lotId1WordOffsetText),
            ParseIntOrZero(lotId2WordOffsetText),
            ParseIntOrZero(lotIdWordLengthText));

        if (!executeRequested)
        {
            return new PilotSmokeConfiguration(
                ShouldExecuteRead: false,
                SkipReason: $"Actual PLC read skipped. Pass {ExecuteReadOnlyFlag} after confirming the target is read-only safe.",
                connection,
                read,
                layout);
        }

        var missing = FindMissing(
            ("--host", host),
            ("--port", portText),
            ("--read-start", readStart),
            ("--read-word-count", readWordCountText),
            ("--start-signal-word-index", startSignalWordIndexText),
            ("--lot-id1-word-offset", lotId1WordOffsetText),
            ("--lot-id2-word-offset", lotId2WordOffsetText),
            ("--lot-id-word-length", lotIdWordLengthText));
        if (missing.Count > 0)
        {
            return new PilotSmokeConfiguration(
                ShouldExecuteRead: false,
                SkipReason: "Actual PLC read skipped. Missing required read-only configuration: " + string.Join(", ", missing),
                connection,
                read,
                layout);
        }

        var invalid = FindInvalidNumericValues(
            ("--port", portText, Min: 1, Max: 65535),
            ("--read-word-count", readWordCountText, Min: 1, Max: int.MaxValue),
            ("--start-signal-word-index", startSignalWordIndexText, Min: 0, Max: int.MaxValue),
            ("--lot-id1-word-offset", lotId1WordOffsetText, Min: 0, Max: int.MaxValue),
            ("--lot-id2-word-offset", lotId2WordOffsetText, Min: 0, Max: int.MaxValue),
            ("--lot-id-word-length", lotIdWordLengthText, Min: 1, Max: int.MaxValue));
        if (invalid.Count > 0)
        {
            return new PilotSmokeConfiguration(
                ShouldExecuteRead: false,
                SkipReason: "Actual PLC read skipped. Invalid numeric configuration: " + string.Join(", ", invalid),
                connection,
                read,
                layout);
        }

        return new PilotSmokeConfiguration(
            ShouldExecuteRead: true,
            SkipReason: null,
            connection,
            read,
            layout);
    }

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[arg] = args[i + 1];
                i++;
            }
            else
            {
                values[arg] = null;
            }
        }

        return values;
    }

    private static string? GetValue(
        IReadOnlyDictionary<string, string?> values,
        string argName,
        Func<string, string?> getEnvironmentVariable,
        string environmentVariableName)
    {
        if (values.TryGetValue(argName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var environmentValue = getEnvironmentVariable(environmentVariableName);
        return string.IsNullOrWhiteSpace(environmentValue) ? null : environmentValue;
    }

    private static int ParseIntOrZero(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : 0;

    private static List<string> FindMissing(params (string Name, string? Value)[] values) =>
        values
            .Where(value => string.IsNullOrWhiteSpace(value.Value))
            .Select(value => value.Name)
            .ToList();

    private static List<string> FindInvalidNumericValues(
        params (string Name, string? Value, int Min, int Max)[] values) =>
        values
            .Where(value =>
                !int.TryParse(value.Value, out var parsed)
                || parsed < value.Min
                || parsed > value.Max)
            .Select(value => value.Name)
            .ToList();
}
