//   Database Adapters - Fatum Adapters for SQL Databases 
//
//   Copyright (C) 2003-2023 Eric Knight
//   This software is distributed under the GNU Public v3 License
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.

//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.

//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Data;
using Proliferation.Fatum;

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
