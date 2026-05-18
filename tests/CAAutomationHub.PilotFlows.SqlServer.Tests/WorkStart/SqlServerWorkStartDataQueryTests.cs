using System.Data;
using CAAutomationHub.PilotFlows.SqlServer.WorkStart;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.SqlServer.Tests.WorkStart;

public sealed class SqlServerWorkStartDataQueryTests
{
    [Fact]
    public void Constructor_RejectsBlankConnectionString()
    {
        var options = new SqlServerWorkStartDataQueryOptions
        {
            ConnectionString = " ",
            SqlText = WorkStartSqlQueryText.Default
        };

        var error = Assert.Throws<ArgumentException>(() => new SqlServerWorkStartDataQuery(options));

        Assert.Contains("connection string", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsSqlTextWithoutLotIdParameter()
    {
        var options = new SqlServerWorkStartDataQueryOptions
        {
            ConnectionString = "placeholder-connection-token",
            SqlText = "SELECT PROFILE FROM WorkDataList WHERE LotId = 'literal'"
        };

        var error = Assert.Throws<ArgumentException>(() => new SqlServerWorkStartDataQuery(options));

        Assert.Contains("@LotId", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultQuery_UsesLotIdParameter()
    {
        Assert.Contains("@LotId", WorkStartSqlQueryText.Default, StringComparison.Ordinal);
        Assert.DoesNotContain("{lotId", WorkStartSqlQueryText.Default, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$\"", WorkStartSqlQueryText.Default, StringComparison.Ordinal);
    }

    [Fact]
    public void Mapper_MapsExpectedColumnsAndNullNumbers()
    {
        var record = new DictionaryDataRecord(new Dictionary<string, object?>
        {
            ["PROFILE"] = "P-100",
            ["TBLR"] = "T",
            ["WIN_TYPE"] = "W",
            ["CUT_SIZE"] = 1250,
            ["LR"] = DBNull.Value,
            ["RollerYN"] = "Y",
            ["ROLLER_HOLE_POS"] = 12.6m,
            ["ROLLER_HOLE_WIDTH"] = DBNull.Value,
            ["ROLLER_HOLE_LENGTH"] = 34,
            ["ROLLER_TYPE"] = "S",
            ["CUT_DEGREE"] = 45
        });

        var data = SqlServerWorkStartDataQueryMapper.Map("LOT-001", record);

        Assert.Equal("LOT-001", data.LotId);
        Assert.Equal("P-100", data.Profile);
        Assert.Equal("T", data.Tblr);
        Assert.Equal("W", data.WinType);
        Assert.Equal(1250, data.CutSize);
        Assert.Equal(string.Empty, data.Lr);
        Assert.Equal("Y", data.RollerYn);
        Assert.Equal(13, data.RollerHolePos);
        Assert.Equal(0, data.RollerHoleWidth);
        Assert.Equal(34, data.RollerHoleLength);
        Assert.Equal("S", data.RollerType);
        Assert.Equal(45, data.CutDegree);
    }

    private sealed class DictionaryDataRecord : IDataRecord
    {
        private readonly IReadOnlyList<string> _names;
        private readonly IReadOnlyDictionary<string, object?> _values;

        public DictionaryDataRecord(IReadOnlyDictionary<string, object?> values)
        {
            _names = values.Keys.ToArray();
            _values = values;
        }

        public int FieldCount => _names.Count;

        public object this[int i] => GetValue(i);

        public object this[string name] => _values[name] ?? DBNull.Value;

        public string GetName(int i) => _names[i];

        public int GetOrdinal(string name) => _names
            .Select((value, index) => new { value, index })
            .First(item => item.value.Equals(name, StringComparison.OrdinalIgnoreCase))
            .index;

        public object GetValue(int i) => _values[_names[i]] ?? DBNull.Value;

        public bool IsDBNull(int i) => GetValue(i) == DBNull.Value;

        public string GetString(int i) => (string)GetValue(i);

        public int GetInt32(int i) => (int)GetValue(i);

        public long GetInt64(int i) => (long)GetValue(i);

        public bool GetBoolean(int i) => (bool)GetValue(i);

        public byte GetByte(int i) => (byte)GetValue(i);

        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();

        public char GetChar(int i) => (char)GetValue(i);

        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();

        public IDataReader GetData(int i) => throw new NotSupportedException();

        public string GetDataTypeName(int i) => GetFieldType(i).Name;

        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);

        public decimal GetDecimal(int i) => (decimal)GetValue(i);

        public double GetDouble(int i) => (double)GetValue(i);

        public Type GetFieldType(int i) => GetValue(i).GetType();

        public float GetFloat(int i) => (float)GetValue(i);

        public Guid GetGuid(int i) => (Guid)GetValue(i);

        public short GetInt16(int i) => (short)GetValue(i);

        public int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var index = 0; index < count; index++)
            {
                values[index] = GetValue(index);
            }

            return count;
        }
    }
}
