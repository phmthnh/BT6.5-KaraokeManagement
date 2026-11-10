using System.Data;
using Microsoft.Data.SqlClient;

namespace KaraokeManagement.DAL;

internal static class Db
{
    internal static SqlParameter Int(string key,int value) => DatabaseHelper.P(key,SqlDbType.Int,value);
    internal static SqlParameter Text(string key,string? value,int size = -1) => new(key,SqlDbType.NVarChar,size) { Value=(object?)value ?? DBNull.Value };
    internal static SqlParameter Code(string key,string value,int size) => DatabaseHelper.P(key,SqlDbType.VarChar,value,size);
    internal static SqlParameter Money(string key,decimal value) => DatabaseHelper.P(key,SqlDbType.Decimal,value);
    internal static SqlParameter Date(string key,DateTime value) => DatabaseHelper.P(key,SqlDbType.DateTime,value);
    internal static T Transaction<T>(Func<SqlConnection,SqlTransaction,T> action)
    {
        using var connection = DatabaseHelper.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = action(connection,transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            if (transaction.Connection is not null) transaction.Rollback();
            throw;
        }
    }

    internal static SqlCommand Command(SqlConnection connection,SqlTransaction transaction,string sql,params SqlParameter[] parameters)
    {
        var command = new SqlCommand(sql,connection,transaction);
        command.Parameters.AddRange(parameters);
        return command;
    }

    internal static object? Scalar(SqlConnection connection,SqlTransaction transaction,string sql,params SqlParameter[] parameters)
    {
        using var command=Command(connection,transaction,sql,parameters);
        return command.ExecuteScalar();
    }

    internal static int Execute(SqlConnection connection,SqlTransaction transaction,string sql,params SqlParameter[] parameters)
    {
        using var command=Command(connection,transaction,sql,parameters);
        return command.ExecuteNonQuery();
    }
}
