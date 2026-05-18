using System.Buffers.Binary;

namespace CAAutomationHub.PilotFlows.WorkComplete;

public static class WorkCompleteReadBlockInterpreter
{
    public static bool IsCompleteSignalActive(byte[] readBlockBytes, int completeSignalWordIndex)
    {
        ArgumentNullException.ThrowIfNull(readBlockBytes);

        if (completeSignalWordIndex < 0)
        {
            return false;
        }

        var byteOffset = checked(completeSignalWordIndex * 2);
        if (byteOffset > readBlockBytes.Length - sizeof(ushort))
        {
            return false;
        }

        var statusWord = BinaryPrimitives.ReadUInt16LittleEndian(readBlockBytes.AsSpan(byteOffset, 2));
        return statusWord != 0;
    }
}
