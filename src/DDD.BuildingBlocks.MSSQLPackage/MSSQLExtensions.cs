using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DDD.BuildingBlocks.MSSQLPackage
{
    /// <summary>
    ///     Sql specific utility extensions.
    /// </summary>
    public static class MSSQLExtensions
    {
        public static byte[] Combine(this byte[]? first, byte[] second)
        {
            first ??= [];

            var ret = new byte[first.Length + second.Length];

            if(first.Length != 0)
            {
                Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            }

            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static byte[] Combine(this byte[] first, byte[] second, byte[] third)
        {
            var ret = new byte[first.Length + second.Length + third.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
                third.Length);
            return ret;
        }

        public static SqlParameter ToSqlParameter(this DateTime value, string name)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                Value = value
            };

            return parameter;
        }

        public static SqlParameter ToSqlParameter(this decimal value, string name)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                Value = value
            };

            return parameter;
        }

        public static SqlParameter ToSqlParameter(this int value, string name)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name, Value = value
            };

            return parameter;
        }

        public static SqlParameter ToSqlParameter(this string value, string name)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                Value = value
            };

            return parameter;
        }

        public static SqlParameter ToSqlParameter(this Guid value, string name)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                Size = value.ToByteArray().Length,
                Value = value.ToByteArray()
            };

            return parameter;
        }

        public static SqlParameter ToSqlParameter(this byte[] value, string name)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                Direction = ParameterDirection.Input,
                Value = value,
                Size = value.Length
            };

            return parameter;
        }
    }
}
