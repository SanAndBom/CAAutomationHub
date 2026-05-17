using System.Buffers.Binary;
using System.Text;

namespace CAAutomationHub.PilotFlows.WorkStart;

public static class WorkStartReadBlockInterpreter
{
    public static bool IsStartSignalActive(byte[] readBlockBytes, int startSignalWordIndex)
    {
        ArgumentNullException.ThrowIfNull(readBlockBytes);

        if (!TryGetByteRange(readBlockBytes, startSignalWordIndex, 1, out var byteOffset, out _))
        {
            return false;
        }

        var statusWord = BinaryPrimitives.ReadUInt16LittleEndian(readBlockBytes.AsSpan(byteOffset, 2));
        return statusWord != 0;
    }

    public static WorkStartLotIdExtractionResult ExtractLotId(
        byte[] readBlockBytes,
        int wordOffset,
        int wordLength)
    {
        ArgumentNullException.ThrowIfNull(readBlockBytes);

        if (!TryGetByteRange(readBlockBytes, wordOffset, wordLength, out var byteOffset, out var byteLength))
        {
            return new WorkStartLotIdExtractionResult
            {
                LotId = string.Empty,
                WordOffset = wordOffset,
                WordLength = wordLength,
                IsInRange = false
            };
        }

        var ascii = Encoding.ASCII.GetString(readBlockBytes, byteOffset, byteLength);
        return new WorkStartLotIdExtractionResult
        {
            LotId = ascii.Trim('\0', ' '),
            WordOffset = wordOffset,
            WordLength = wordLength,
            IsInRange = true
        };
    }

    public static WorkStartLotIdSelectionResult SelectLotId(string? lotId1, string? lotId2)
    {
        if (!string.IsNullOrWhiteSpace(lotId1))
        {
            return new WorkStartLotIdSelectionResult
            {
                SelectedLotId = lotId1,
                Source = WorkStartLotIdSelectionSource.LotId1
            };
        }

        if (!string.IsNullOrWhiteSpace(lotId2))
        {
            return new WorkStartLotIdSelectionResult
            {
                SelectedLotId = lotId2,
                Source = WorkStartLotIdSelectionSource.LotId2
            };
        }

        return new WorkStartLotIdSelectionResult
        {
            SelectedLotId = null,
            Source = WorkStartLotIdSelectionSource.None
        };
    }

    private static bool TryGetByteRange(
        byte[] readBlockBytes,
        int wordOffset,
        int wordLength,
        out int byteOffset,
        out int byteLength)
    {
        byteOffset = 0;
        byteLength = 0;

        if (wordOffset < 0 || wordLength < 0)
        {
            return false;
        }

        byteOffset = checked(wordOffset * 2);
        byteLength = checked(wordLength * 2);
        return byteOffset <= readBlockBytes.Length && byteLength <= readBlockBytes.Length - byteOffset;
    }
}
