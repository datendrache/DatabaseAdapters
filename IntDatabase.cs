//   Phloz
//   Copyright (C) 2003-2019 Eric Knight

using System;
using System.Data;
using System.Collections.Generic;
using System.Data.Sql;
using System.Data.SqlClient;
using FatumCore;

namespace DatabaseAdapters
{
    public class DatabaseSoftware
    {
        public const int SQLite = 0;
        public const int MicrosoftSQLServer = 1;
    }

    public interface IntDatabase
    {
        int getDatabaseType();
        void setDatabaseSoftware(int softwaretype);
        int getDatabaseSoftware();
        void setDatabaseRevision(int softwarerevision);
        int getDatabaseRevision();
        void setConnectionString(String connectionstring);
        DataTable Execute(string sqlcommand);
        int ExecuteNonQuery(string sqlcommand);
        object ExecuteScalar(string sqlcommand);
        object ExecuteScalarTree(string sqlcommand, Tree data);
        bool UpdateTree(String tableName, Tree data, String where);
        bool DeleteTree(String tableName, Tree data, string where);
        bool InsertTree(String tableName, Tree data);
        bool ClearTable(String table);
        Boolean BeginTransaction();
        Boolean GetTransactionLockStatus();
        Boolean Commit();
        Boolean Rollback();
        String getDatabaseDirectory();
        DataTable GetInversedDataTable(DataTable table, string columnX, params string[] columnsToIgnore);
        DataTable ExecuteDynamic(string sqlcommand, Tree data);
        DataTable ExecuteDynamicWithFile(string sqlcommand, Tree data, string fileField, Byte[] byteData);
        bool Close();
        bool CreateDatabase(String database);
        bool DropDatabase(String database);
        bool CheckDatabaseExists(String database);
        bool InsertPreparedDocument(string[] data);
    }
}
