using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.ApplicationBlocks.Data;

namespace RaaiVan.Modules.GlobalUtilities
{
    public static class ProviderUtil
    {
        private static string _connectionString;
        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                    _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EKMConnectionString"].ConnectionString;
                return _connectionString;
            }
        }

        public static bool succeed(IDataReader reader)
        {
            try
            {
                reader.Read();

                try { return (bool)reader[0]; }
                catch { }

                return long.Parse(reader[0].ToString()) > 0;            }
            catch { return false; }
            finally { if (!reader.IsClosed) reader.Close(); }
        }

        public static DateTime? succeed_datetime(IDataReader reader, ref string errorMessage)
        {
            try
            {
                reader.Read();

                try { if (reader.FieldCount > 1) errorMessage = reader[1].ToString(); }
                catch { }

                return DateTime.Parse(reader[0].ToString());
            }
            catch { return null; }
            finally { if (!reader.IsClosed) reader.Close(); }
        }

        public static DateTime? succeed_datetime(IDataReader reader)
        {
            string msg = string.Empty;
            return succeed_datetime(reader, ref msg);
        }

        public static IDataReader execute_reader(string spName, params object[] parameterValues)
        {
            return (IDataReader)SqlHelper.ExecuteReader(ProviderUtil.ConnectionString, spName, parameterValues);
        }
    }
}
