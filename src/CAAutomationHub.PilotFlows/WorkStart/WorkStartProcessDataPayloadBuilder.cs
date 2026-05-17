using System.Buffers.Binary;
using System.Text;

namespace CAAutomationHub.PilotFlows.WorkStart;

public static class WorkStartProcessDataPayloadBuilder
{
    public static WorkStartPayloadBuildResult Build(
        WorkStartProcessData processData,
        WorkStartPayloadBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(processData);

        var buildOptions = options ?? WorkStartPayloadBuildOptions.Default;
        var payloadLength = checked(buildOptions.WordCount * 2);
        var payload = new byte[payloadLength];
        var fields = new List<WorkStartPayloadField>();

        WriteAsciiFixed(payload, 0, 12, processData.LotId, "LOTID_1", fields, 6);
        WriteAsciiFixed(payload, 20, 12, string.Empty, "LOTID_2", fields, 6);

        WriteAsciiFixed(payload, 80, 18, processData.Profile, "PROFILE", fields, 9);
        WriteSingleAsciiWord(payload, 100, processData.Tblr, "TBLR", fields);
        WriteSingleAsciiWord(payload, 104, processData.WinType, "WIN_TYPE", fields);
        WriteInt32TwoWords(payload, 108, processData.CutSize, "CUT_SIZE", fields);
        WriteSingleAsciiWord(payload, 112, processData.Lr, "LR", fields);
        WriteSingleAsciiWord(payload, 116, processData.RollerYn, "RollerYN", fields);
        WriteInt32TwoWords(payload, 120, processData.RollerHolePos, "ROLLER_HOLE_POS", fields);
        WriteInt32TwoWords(payload, 124, processData.RollerHoleWidth, "ROLLER_HOLE_WIDTH", fields);
        WriteInt32TwoWords(payload, 128, processData.RollerHoleLength, "ROLLER_HOLE_LENGTH", fields);
        WriteSingleAsciiWord(payload, 132, processData.RollerType, "ROLLER_TYPE", fields);
        WriteInt32TwoWords(payload, 136, processData.CutDegree, "CUT_DEGREE", fields);

        return new WorkStartPayloadBuildResult
        {
            PayloadBytes = payload,
            WordCount = buildOptions.WordCount,
            Fields = fields
        };
    }

    private static void WriteAsciiFixed(
        byte[] payload,
        int startByteOffset,
        int length,
        string? value,
        string name,
        List<WorkStartPayloadField> fields,
        int wordLength)
    {
        var sourceBytes = Encoding.ASCII.GetBytes((value ?? string.Empty).Trim());
        var writeLength = Math.Min(length, sourceBytes.Length);

        if (writeLength > 0)
        {
            Buffer.BlockCopy(sourceBytes, 0, payload, startByteOffset, writeLength);
        }

        fields.Add(new WorkStartPayloadField
        {
            Name = name,
            StartWordOffset = startByteOffset / 2,
            WordLength = wordLength,
            Bytes = payload.AsSpan(startByteOffset, length).ToArray()
        });
    }

    private static void WriteSingleAsciiWord(
        byte[] payload,
        int startByteOffset,
        string? value,
        string name,
        List<WorkStartPayloadField> fields)
    {
        payload[startByteOffset] = ExtractFirstAsciiByte(value);
        payload[startByteOffset + 1] = 0;

        fields.Add(new WorkStartPayloadField
        {
            Name = name,
            StartWordOffset = startByteOffset / 2,
            WordLength = 1,
            Bytes = payload.AsSpan(startByteOffset, 2).ToArray()
        });
    }

    private static byte ExtractFirstAsciiByte(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return Encoding.ASCII.GetBytes(value.Trim())[0];
    }

    private static void WriteInt32TwoWords(
        byte[] payload,
        int startByteOffset,
        int value,
        string name,
        List<WorkStartPayloadField> fields)
    {
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(startByteOffset, 4), value);

        fields.Add(new WorkStartPayloadField
        {
            Name = name,
            StartWordOffset = startByteOffset / 2,
            WordLength = 2,
            Bytes = payload.AsSpan(startByteOffset, 4).ToArray()
        });
    }
}
