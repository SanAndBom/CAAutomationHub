using System.Buffers.Binary;
using System.Text;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Tests.WorkStart;

public sealed class WorkStartProcessDataPayloadBuilderTests
{
    [Fact]
    public void BuildsPayload_WithFixedAsciiFields()
    {
        var result = WorkStartProcessDataPayloadBuilder.Build(CreateSampleData());

        Assert.Equal(140, result.PayloadBytes.Length);
        Assert.Equal("LOT123456789", ReadAscii(result.PayloadBytes, 0, 12));
        Assert.True(result.PayloadBytes.AsSpan(20, 12).SequenceEqual(new byte[12]));
        Assert.Equal("PROFILE-ABC", ReadAscii(result.PayloadBytes, 80, 18));
    }

    [Fact]
    public void BuildsPayload_WithSingleCharacterWordFields()
    {
        var data = CreateSampleData() with
        {
            Tblr = " Left",
            WinType = "Window",
            Lr = " Right",
            RollerYn = "Yes",
            RollerType = "Single"
        };

        var result = WorkStartProcessDataPayloadBuilder.Build(data);

        Assert.Equal((byte)'L', result.PayloadBytes[100]);
        Assert.Equal(0, result.PayloadBytes[101]);
        Assert.Equal((byte)'W', result.PayloadBytes[104]);
        Assert.Equal(0, result.PayloadBytes[105]);
        Assert.Equal((byte)'R', result.PayloadBytes[112]);
        Assert.Equal(0, result.PayloadBytes[113]);
        Assert.Equal((byte)'Y', result.PayloadBytes[116]);
        Assert.Equal(0, result.PayloadBytes[117]);
        Assert.Equal((byte)'S', result.PayloadBytes[132]);
        Assert.Equal(0, result.PayloadBytes[133]);
    }

    [Fact]
    public void BuildsPayload_WithTwoWordInt32Fields()
    {
        var result = WorkStartProcessDataPayloadBuilder.Build(CreateSampleData());

        Assert.Equal(500, BinaryPrimitives.ReadInt32LittleEndian(result.PayloadBytes.AsSpan(108, 4)));
        Assert.Equal(1234, BinaryPrimitives.ReadInt32LittleEndian(result.PayloadBytes.AsSpan(120, 4)));
        Assert.Equal(5678, BinaryPrimitives.ReadInt32LittleEndian(result.PayloadBytes.AsSpan(124, 4)));
        Assert.Equal(9012, BinaryPrimitives.ReadInt32LittleEndian(result.PayloadBytes.AsSpan(128, 4)));
        Assert.Equal(90, BinaryPrimitives.ReadInt32LittleEndian(result.PayloadBytes.AsSpan(136, 4)));
    }

    [Fact]
    public void PadsOrTruncatesFixedAsciiFields_AsExpected()
    {
        var data = CreateSampleData() with
        {
            LotId = "  LOT-TOO-LONG-12345  ",
            Profile = "PROFILE-IS-LONGER-THAN-EIGHTEEN"
        };

        var result = WorkStartProcessDataPayloadBuilder.Build(data);

        Assert.Equal("LOT-TOO-LONG", Encoding.ASCII.GetString(result.PayloadBytes, 0, 12));
        Assert.Equal("PROFILE-IS-LONGER-", Encoding.ASCII.GetString(result.PayloadBytes, 80, 18));

        var padded = WorkStartProcessDataPayloadBuilder.Build(CreateSampleData() with { LotId = "LOT", Profile = null });
        Assert.Equal(new byte[] { (byte)'L', (byte)'O', (byte)'T', 0, 0, 0, 0, 0, 0, 0, 0, 0 }, padded.PayloadBytes.AsSpan(0, 12).ToArray());
        Assert.True(padded.PayloadBytes.AsSpan(80, 18).SequenceEqual(new byte[18]));
    }

    [Fact]
    public void ProducesExpectedPayloadLength()
    {
        var result = WorkStartProcessDataPayloadBuilder.Build(CreateSampleData());

        Assert.Equal(WorkStartPayloadBuildOptions.DefaultWordCount, result.WordCount);
        Assert.Equal(WorkStartPayloadBuildOptions.DefaultWordCount * 2, result.PayloadBytes.Length);
    }

    [Fact]
    public void DoesNotReferenceExternalBoundaryAssemblies()
    {
        var forbiddenReferenceNames = new[]
        {
            string.Join('.', "CAAutomationHub", "Run" + "time"),
            string.Join('.', "CAAutomationHub", "Flow" + "Definitions"),
            "AutomationHub." + "X" + "gtDriverCore",
            "X" + "gtChannelRunner",
            "Fake" + "Plc"
        };

        var referencedNames = typeof(WorkStartProcessDataPayloadBuilder)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToArray();

        foreach (var forbiddenReferenceName in forbiddenReferenceNames)
        {
            Assert.DoesNotContain(forbiddenReferenceName, referencedNames);
        }
    }

    private static WorkStartProcessData CreateSampleData() =>
        new()
        {
            LotId = "LOT123456789",
            Profile = "PROFILE-ABC",
            Tblr = "L",
            WinType = "W",
            CutSize = 500,
            Lr = "R",
            RollerYn = "Y",
            RollerHolePos = 1234,
            RollerHoleWidth = 5678,
            RollerHoleLength = 9012,
            RollerType = "S",
            CutDegree = 90
        };

    private static string ReadAscii(byte[] source, int offset, int length) =>
        Encoding.ASCII.GetString(source, offset, length).Trim('\0', ' ');
}
