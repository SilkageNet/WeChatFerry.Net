using Google.Protobuf;
using System.Data;
using System.Text;

namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public List<string> RPCGetDBNames()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetDbNames };
                var res = CallRPC(req);
                return res.Dbs?.Names?.ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger?.Error("GetDbNames failed: {0}", ex);
                return [];
            }
        }

        public List<DbTable> RPCGetDBTables(string db)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetDbTables, Str = db };
                var res = CallRPC(req);
                return res.Tables?.Tables.ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger?.Error("GetDbTables failed: {0}", ex);
                return [];
            }
        }

        internal enum DBFieldType : int
        {
            Unknown = 0,
            Int = 1,
            Single = 2,
            String = 3,
            Bytes = 4,
            Null = 5
        }

        private static object? ConvertDBField(DBFieldType type, ByteString content)
        {
            try
            {
                return type switch
                {
                    DBFieldType.Int => BitConverter.ToInt32(content.ToByteArray()),
                    DBFieldType.Single => BitConverter.ToSingle(content.ToByteArray()),
                    DBFieldType.String => Encoding.UTF8.GetString(content.ToByteArray()),
                    DBFieldType.Bytes => content.ToByteArray(),
                    DBFieldType.Null => null,
                    _ => content.ToStringUtf8()
                };
            }
            catch
            {
                return content.ToStringUtf8();
            }
        }

        private static Type GetDBFieldColumnType(DBFieldType type) => type switch
        {
            DBFieldType.Int => typeof(int),
            DBFieldType.Single => typeof(float),
            DBFieldType.String => typeof(string),
            DBFieldType.Bytes => typeof(byte[]),
            DBFieldType.Null => typeof(object),
            _ => typeof(string)
        };

        public DataTable? RPCExecDBQuery(string db, string sql)
        {
            try
            {
                var req = new Request { Func = Functions.FuncExecDbQuery, Query = new DbQuery { Db = db, Sql = sql } };
                var res = CallRPC(req);
                if (res.Rows == null)
                {
                    _logger?.Error("ExecDbQuery failed, rows is null");
                    return null;
                }
                var dt = new DataTable();
                foreach (var r in res.Rows.Rows)
                {
                    var row = dt.NewRow();
                    foreach (var item in r.Fields)
                    {
                        var type = (DBFieldType)item.Type;
                        if (!dt.Columns.Contains(item.Column)) dt.Columns.Add(item.Column, GetDBFieldColumnType(type));
                        row[item.Column] = ConvertDBField(type, item.Content);
                    }
                    dt.Rows.Add(row);
                }
                return dt;
            }
            catch (Exception ex)
            {
                _logger?.Error("ExecDBQuery failed: {0}", ex);
                return null;
            }
        }

        public List<Dictionary<string, object?>>? RPCExecDBQueryOutputDict(string db, string sql)
        {
            try
            {
                var req = new Request { Func = Functions.FuncExecDbQuery, Query = new DbQuery { Db = db, Sql = sql } };
                var res = CallRPC(req);
                if (res.Rows == null)
                {
                    _logger?.Error("ExecDbQuery failed, rows is null");
                    return null;
                }
                var result = new List<Dictionary<string, object?>>();
                foreach (var r in res.Rows.Rows)
                {
                    var row = new Dictionary<string, object?>();
                    foreach (var item in r.Fields)
                    {
                        row[item.Column] = ConvertDBField((DBFieldType)item.Type, item.Content);
                    }
                    result.Add(row);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error("ExecDBQueryOutputDict failed: {0}", ex);
                return null;
            }
        }
    }
}
