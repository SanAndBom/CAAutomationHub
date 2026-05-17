using System.Buffers.Binary;
using System.Text;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Tests.WorkStart;

public sealed class WorkStartReadBlockInterpreterTests
{
    [Fact]
    public void DetectsStartSignalActive_WhenWordIsNonZero()
    {
        var readBlockBytes = CreateReadBlock();
        BinaryPrimitives.WriteUInt16LittleEndian(
            readBlockBytes.AsSpan(WorkStartReadBlockLayout.DefaultStartSignalWordIndex * 2, 2),
            1);

        var isActive = WorkStartReadBlockInterpreter.IsStartSignalActive(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultStartSignalWordIndex);

        Assert.True(isActive);
    }

    [Fact]
    public void DetectsStartSignalInactive_WhenWordIsZero()
    {
        var readBlockBytes = CreateReadBlock();

        var isActive = WorkStartReadBlockInterpreter.IsStartSignalActive(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultStartSignalWordIndex);

        Assert.False(isActive);
    }

    [Fact]
    public void TreatsOutOfRangeStartSignalIndexAsInactive()
    {
        var readBlockBytes = CreateReadBlock();

        var isActive = WorkStartReadBlockInterpreter.IsStartSignalActive(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultReadWordCount);

        Assert.False(isActive);
    }

    [Fact]
    public void ExtractsLotId_FromAsciiWords()
    {
        var readBlockBytes = CreateReadBlock();
        WriteAscii(readBlockBytes, WorkStartReadBlockLayout.DefaultLotId1WordOffset, "LOT123456789");

        var result = WorkStartReadBlockInterpreter.ExtractLotId(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultLotId1WordOffset,
            WorkStartReadBlockLayout.DefaultLotIdWordLength);

        Assert.Equal("LOT123456789", result.LotId);
        Assert.True(result.IsInRange);
    }

    [Fact]
    public void TrimsNullAndWhitespace_FromLotId()
    {
        var readBlockBytes = CreateReadBlock();
        WriteBytes(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultLotId1WordOffset,
            new byte[] { 0, (byte)' ', (byte)'L', (byte)'O', (byte)'T', (byte)'4', (byte)'2', (byte)' ', 0, 0, 0, 0 });

        var result = WorkStartReadBlockInterpreter.ExtractLotId(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultLotId1WordOffset,
            WorkStartReadBlockLayout.DefaultLotIdWordLength);

        Assert.Equal("LOT42", result.LotId);
        Assert.True(result.IsInRange);
    }

    [Fact]
    public void ReturnsEmptyLotId_WhenRangeIsOutOfBounds()
    {
        var readBlockBytes = CreateReadBlock();

        var result = WorkStartReadBlockInterpreter.ExtractLotId(
            readBlockBytes,
            WorkStartReadBlockLayout.DefaultReadWordCount,
            WorkStartReadBlockLayout.DefaultLotIdWordLength);

        Assert.Equal(string.Empty, result.LotId);
        Assert.False(result.IsInRange);
    }

    [Fact]
    public void SelectsLotId1_WhenBothLotIdsExist()
    {
        var result = WorkStartReadBlockInterpreter.SelectLotId("LOT-1", "LOT-2");

        Assert.True(result.HasSelection);
        Assert.Equal("LOT-1", result.SelectedLotId);
        Assert.Equal(WorkStartLotIdSelectionSource.LotId1, result.Source);
    }

    [Fact]
    public void SelectsLotId2_WhenLotId1IsEmpty()
    {
        var result = WorkStartReadBlockInterpreter.SelectLotId(" ", "LOT-2");

        Assert.True(result.HasSelection);
        Assert.Equal("LOT-2", result.SelectedLotId);
        Assert.Equal(WorkStartLotIdSelectionSource.LotId2, result.Source);
    }

    [Fact]
    public void ReturnsNoSelection_WhenBothLotIdsAreEmpty()
    {
        var result = WorkStartReadBlockInterpreter.SelectLotId(null, string.Empty);

        Assert.False(result.HasSelection);
        Assert.Null(result.SelectedLotId);
        Assert.Equal(WorkStartLotIdSelectionSource.None, result.Source);
    }

    private static byte[] CreateReadBlock() =>
        new byte[WorkStartReadBlockLayout.DefaultReadWordCount * 2];

    private static void WriteAscii(byte[] readBlockBytes, int wordOffset, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        WriteBytes(readBlockBytes, wordOffset, bytes);
    }

    private static void WriteBytes(byte[] readBlockBytes, int wordOffset, byte[] bytes)
    {
        Buffer.BlockCopy(bytes, 0, readBlockBytes, wordOffset * 2, bytes.Length);
    }
}
