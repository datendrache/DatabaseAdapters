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
using System.Data.SqlClient;
using Proliferation.Fatum;

namespace DatabaseAdapters
{
    public class ADOConnector : IntDatabase
    {
        String dbConnection;
        public SqlConnection dbCursor;
        public string ConnectionString = "";
        private SqlTransaction transaction = null;
        private Boolean transactionLock = false;
        int SoftwareType = 1;
        int SoftwareRevision = 0;

        /// <summary>
        ///     Default Constructor for SQLiteDatabase Class.
        /// </summary>

        public ADOConnector(String connectionString, String DatabaseOverride)
        {
            Boolean successful = false;

            while (!successful)
            {
                try
                {
                    ConnectionString = connectionString;
                    dbCursor = new SqlConnection(connectionString);
                    dbCursor.Open();
                    ExecuteNonQuery("Use " + DatabaseOverride + ";");
                    successful = true;
                }
                catch (Exception xyz)
                {
                    System.Console.Out.WriteLine(xyz.Message);
                    System.Console.Out.WriteLine(xyz.StackTrace);
                    Thread.Sleep(5000);  // If failed to connect, wait 5 seconds and try again.
                }
            }
        }

        public int getDatabaseType()
        {
            return DatabaseSoftware.MicrosoftSQLServer;
        }

        public void setConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public Boolean Close()
        {
            if (dbCursor != null)
            {
                dbCursor.Close();
                dbCursor = null;
            }
            return true;
        }

        /// <summary>
        /// Gets a Inverted DataTable
        /// </summary>
        /// <param name="table">DataTable do invert</param>
        /// <param name="columnX">X Axis Column</param>
        /// <param name="nullValue">null Value to Complete the Pivot Table</param>
        /// <param name="columnsToIgnore">Columns that should be ignored in the pivot 
        /// process (X Axis column is ignored by default)</param>
        /// <returns>C# Pivot Table Method  - Felipe Sabino</returns>
        public DataTable GetInversedDataTable(DataTable table, string columnX,
                                                     params string[] columnsToIgnore)
        {
            //Create a DataTable to Return
            DataTable returnTable = new DataTable();

            if (columnX == "")
                columnX = table.Columns[0].ColumnName;

            //Add a Column at the beginning of the table

            returnTable.Columns.Add(columnX);

            //Read all DISTINCT values from columnX Column in the provided DataTale
            List<string> columnXValues = new List<string>();

            //Creates list of columns to ignore
            List<string> listColumnsToIgnore = new List<string>();
            if (columnsToIgnore.Length > 0)
                listColumnsToIgnore.AddRange(columnsToIgnore);

            if (!listColumnsToIgnore.Contains(columnX))
                listColumnsToIgnore.Add(columnX);

            foreach (DataRow dr in table.Rows)
            {
                string columnXTemp = dr[columnX].ToString();
                //Verify if the value was already listed
                if (!columnXValues.Contains(columnXTemp))
                {
                    //if the value id different from others provided, add to the list of 
                    //values and creates a new Column with its value.
                    columnXValues.Add(columnXTemp);
                    returnTable.Columns.Add(columnXTemp);
                }
                else
                {
                    //Throw exception for a repeated value
                    throw new Exception("The inversion used must have " +
                                        "unique values for column " + columnX);
                }
            }

            //Add a line for each column of the DataTable

            foreach (DataColumn dc in table.Columns)
            {
                if (!columnXValues.Contains(dc.ColumnName) &&
                    !listColumnsToIgnore.Contains(dc.ColumnName))
                {
                    DataRow dr = returnTable.NewRow();
                    dr[0] = dc.ColumnName;
                    returnTable.Rows.Add(dr);
                }
            }

            //Complete the datatable with the values
            for (int i = 0; i < returnTable.Rows.Count; i++)
            {
                for (int j = 1; j < returnTable.Columns.Count; j++)
                {
                    returnTable.Rows[i][j] =
                      table.Rows[j - 1][returnTable.Rows[i][0].ToString()].ToString();
                }
            }

            return returnTable;
        }

        /// <summary>
        ///     Single Param Constructor for specifying advanced connection options.
        /// </summary>
        /// <param name="connectionOpts">A dictionary containing all desired options and their values</param>

        /// <summary>
        /// Gets a Inverted DataTable
        /// </summary>
        /// <param name="table">Provided DataTable</param>
        /// <param name="columnX">X Axis Column</param>
        /// <param name="columnY">Y Axis Column</param>
        /// <param name="columnZ">Z Axis Column (values)</param>
        /// <param name="columnsToIgnore">Whether to ignore some column, it must be 
        /// provided here</param>
        /// <param name="nullValue">null Values to be filled</param> 
        /// <returns>C# Pivot Table Method  - Felipe Sabino</returns>
        public static DataTable GetInversedDataTable(DataTable table, string columnX,
             string columnY, string columnZ, string nullValue, bool sumValues)
        {
            //Create a DataTable to Return
            DataTable returnTable = new DataTable();

            if (columnX == "")
                columnX = table.Columns[0].ColumnName;

            //Add a Column at the beginning of the table
            returnTable.Columns.Add(columnY);


            //Read all DISTINCT values from columnX Column in the provided DataTale
            List<string> columnXValues = new List<string>();

            foreach (DataRow dr in table.Rows)
            {

                string columnXTemp = dr[columnX].ToString();
                if (!columnXValues.Contains(columnXTemp))
                {
                    //Read each row value, if it's different from others provided, add to 
                    //the list of values and creates a new Column with its value.
                    columnXValues.Add(columnXTemp);
                    returnTable.Columns.Add(columnXTemp);
                }
            }

            //Verify if Y and Z Axis columns re provided
            if (columnY != "" && columnZ != "")
            {
                //Read DISTINCT Values for Y Axis Column
                List<string> columnYValues = new List<string>();

                foreach (DataRow dr in table.Rows)
                {
                    if (!columnYValues.Contains(dr[columnY].ToString()))
                        columnYValues.Add(dr[columnY].ToString());
                }

                //Loop all Column Y Distinct Value
                foreach (string columnYValue in columnYValues)
                {
                    //Creates a new Row
                    DataRow drReturn = returnTable.NewRow();
                    drReturn[0] = columnYValue;
                    //foreach column Y value, The rows are selected distincted
                    DataRow[] rows = table.Select(columnY + "='" + columnYValue + "'");

                    //Read each row to fill the DataTable
                    foreach (DataRow dr in rows)
                    {
                        string rowColumnTitle = dr[columnX].ToString();

                        //Read each column to fill the DataTable
                        foreach (DataColumn dc in returnTable.Columns)
                        {
                            if (dc.ColumnName == rowColumnTitle)
                            {
                                //If Sum of Values is True it try to perform a Sum
                                //If sum is not possible due to value types, the value 
                                // displayed is the last one read
                                if (sumValues)
                                {
                                    try
                                    {
                                        drReturn[rowColumnTitle] =
                                             Convert.ToDecimal(drReturn[rowColumnTitle]) +
                                             Convert.ToDecimal(dr[columnZ]);
                                    }
                                    catch
                                    {
                                        drReturn[rowColumnTitle] = dr[columnZ];
                                    }
                                }
                                else
                                {
                                    drReturn[rowColumnTitle] = dr[columnZ];
                                }
                            }
                        }
                    }
                    returnTable.Rows.Add(drReturn);
                }
            }
            else
            {
                throw new Exception("The columns to perform inversion are not provided");
            }

            //if a nullValue is provided, fill the datable with it
            if (nullValue != "")
            {
                foreach (DataRow dr in returnTable.Rows)
                {
                    foreach (DataColumn dc in returnTable.Columns)
                    {
                        if (dr[dc.ColumnName].ToString() == "")
                            dr[dc.ColumnName] = nullValue;
                    }
                }
            }

            return returnTable;
        }

        public ADOConnector(Dictionary<String, String> connectionOpts)
        {
            String str = "";

            foreach (KeyValuePair<String, String> row in connectionOpts)
            {
                str += String.Format("{0}={1}; ", row.Key, row.Value);
            }
            str = str.Trim().Substring(0, str.Length - 1);
            dbConnection = str;
        }

        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>

        public DataTable Execute(string sql)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlCommand mycommand = new SqlCommand();
                mycommand.Connection = dbCursor;
                mycommand.CommandText = sql;
                if (transaction != null)
                {
                    mycommand.Transaction = transaction;
                }
                SqlDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
            }

            catch (Exception e)
            {
                System.Console.Out.WriteLine(e.Message);
                System.Console.Out.WriteLine(e.StackTrace);
                throw new Exception(e.Message);
            }

            return dt;
        }

        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>

        public int ExecuteNonQuery(string sql)
        {
            SqlCommand mycommand = new SqlCommand();
            mycommand.Connection = dbCursor;
            mycommand.CommandText = sql;
            if (transaction != null)
            {
                mycommand.Transaction = transaction;
            }
            int rowsUpdated = mycommand.ExecuteNonQuery();
            return rowsUpdated;
        }

        /// <summary>
        ///     Allows the programmer to retrieve single items from the DB.
        /// </summary>
        /// <param name="sql">The query to run.</param>
        /// <returns>A string.</returns>

        public object ExecuteScalar(string sql)
        {
            SqlCommand mycommand = new SqlCommand();
            mycommand.Connection = dbCursor;
            mycommand.CommandText = sql;
            if (transaction != null)
            {
                mycommand.Transaction = transaction;
            }
            object value = mycommand.ExecuteScalar();
            if (value != null)
            {
                return value;
            }
            return "";
        }

        public object ExecuteScalarTree(string sql, Tree data)
        {
            SqlCommand mycommand = new SqlCommand();
            mycommand.Connection = dbCursor;
            mycommand.CommandText = sql;
            int indyntreeCount = data.tree.Count;
            for (int index = 0; index < indyntreeCount; index++)
            {
                string key = (string)data.leafnames[index];
                string value = data.GetElement(key);
                mycommand.Parameters.Add(new SqlParameter(key, value));
            }

            if (transaction != null)
            {
                mycommand.Transaction = transaction;
            }
            object result = mycommand.ExecuteScalar();
            if (result != null)
            {
                return result;
            }
            return "";
        }

        /// <summary>
        ///     Allows the programmer to easily update rows in the DB.
        /// </summary>
        /// <param name="tableName">The table to update.</param>
        /// <param name="data">A dictionary containing Column names and their new values.</param>
        /// <param name="where">The where clause for the update statement.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>

        public bool UpdateTree(String tableName, Tree data, String where)
        {
            String values = "";
            Boolean returnCode = true;
            Tree parms = new Tree();

            int datatreeCount = data.tree.Count;
            for (int i = 0; i < datatreeCount; i++)
            {
                string key = (string)data.leafnames[i];
                string value = data.GetElement(key);

                if (key.Substring(0, 1) != "_")
                {
                    if (key.Substring(0, 1) != "*")
                    {
                        Tree casting = data.FindNode("_" + key);

                        if (casting != null)  // This key is typecast
                        {

                            switch (casting.Value.ToLower())
                            {
                                case "bigint":
                                    values += String.Format(" [{0}]=CONVERT(bigint,{1}),", key, "@value" + i.ToString());
                                    parms.AddElement("@value" + i.ToString(), value);
                                    break;
                                case "smallint":
                                    values += String.Format(" [{0}]=CONVERT(smallint,{1}),", key, "@value" + i.ToString());
                                    parms.AddElement("@value" + i.ToString(), value);
                                    break;
                                case "real":
                                    values += String.Format(" [{0}]=CONVERT(real,{1}),", key, "@value" + i.ToString());
                                    parms.AddElement("@value" + i.ToString(), value);
                                    break;
                                case "float":
                                    values += String.Format(" [{0}]=CONVERT(float,{1}),", key, "@value" + i.ToString());
                                    parms.AddElement("@value" + i.ToString(), value);
                                    break;
                                default:
                                    values += String.Format(" [{0}]={1},", key, "@value" + i.ToString());
                                    parms.AddElement("@value" + i.ToString(), value);
                                    break;
                            }
                        }
                        else
                        {
                            values += String.Format(" [{0}]={1},", key, "@value" + i.ToString());
                            parms.AddElement("@value" + i.ToString(), value);
                        }
                    }
                    else
                    {
                        parms.AddElement(key.Substring(1), value);
                    }
                  
                }    
            }
            values = values.Substring(0, values.Length - 1);

            try
            {
                if (where!="")
                {
                    this.ExecuteDynamic(String.Format("update {0} set {1} where {2};", tableName, values, where), parms);
                }
                else
                {
                    this.ExecuteDynamic(String.Format("update {0} set {1};", tableName, values), parms);
                }
            }
            catch (Exception xyz)
            {
                System.Console.Out.WriteLine(xyz.Message);
                System.Console.Out.WriteLine(xyz.StackTrace);
                returnCode = false;
            }
            parms.Dispose();
            return returnCode;
        }

        public bool DeleteTree(String tableName, Tree data, String where)
        {
            Boolean returnCode = true;
 
            try
            {
                this.ExecuteDynamic(String.Format("delete from {0} where {1};", tableName, where), data);
            }
            catch (Exception)
            {
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily insert into the DB
        /// </summary>
        /// <param name="tableName">The table into which we insert the data.</param>
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>

        public bool InsertTree(String tableName, Tree data)
        {
            String columns = "";
            String values = "";
            Boolean returnCode = true;
            Tree parms = new Tree();

            int datatreeCount = data.tree.Count;
            for (int i = 0; i < datatreeCount; i++)
            {
                string key = (string) data.leafnames[i];
                string value = data.GetElement(key);

                if (key.Substring(0, 1) != "_")
                {
                    if (key.Substring(0, 1) != "*")
                    {
                        Tree casting = data.FindNode("_" + key);

                        if (casting != null)  // This key is typecast
                        {
                            columns += String.Format(" [{0}],", key);


                            switch (casting.Value.ToLower())
                            {
                                case "bigint":
                                    values += " CONVERT(bigint,@value" + i.ToString() + "),";
                                    break;
                                case "smallint":
                                    values += " CONVERT(smallint,@value" + i.ToString() + "),";
                                    break;
                                case "integer":
                                    values += " CONVERT(integer,@value" + i.ToString() + "),";
                                    break;
                                case "real":
                                    values += " CONVERT(real,@value" + i.ToString() + "),";
                                    break;
                                case "float":
                                    values += " CONVERT(float,@value" + i.ToString() + "),";
                                    break;
                                default:
                                    values += " @value" + i.ToString() + ",";
                                    break;
                            }
                            parms.AddElement("@value" + i.ToString(), value);
                        }
                        else
                        {
                            columns += String.Format(" [{0}],", key);
                            values += " @value" + i.ToString() + ",";
                            parms.AddElement("@value" + i.ToString(), value);
                        }
                    }
                    else
                    {
                        parms.AddElement(key.Substring(1), value);
                    }
                }
            }

            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);

            try
            {
                this.ExecuteDynamic(String.Format("insert into {0}({1}) values({2});", tableName, columns, values),parms);
            }
            catch (Exception xyz)
            {
                System.Console.Out.WriteLine(xyz.Message);
                System.Console.Out.WriteLine(xyz.StackTrace);
                returnCode = false;
            }
            parms.Dispose();
            return returnCode;
        }

        /// <summary>
        ///     Allows the user to easily clear all data from a specific table.
        /// </summary>
        /// <param name="table">The name of the table to clear.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>

        public bool ClearTable(String table)
        {
            try
            {
                this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Boolean BeginTransaction()
        {
            Boolean result = true;

            try
            {
                while (transactionLock)
                {
                    System.Threading.Thread.Sleep(5);  // Something is going on so lets give it some time.
                }
                transactionLock = true;
                System.Threading.Thread.Sleep(0);
                transaction = dbCursor.BeginTransaction();
            }
            catch (Exception xyz)
            {
                result = false;
            }
            return result;
        }


        public Boolean Commit()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Commit();
                    transaction.Dispose();
                    transaction = null;
                }
                transactionLock = false;
                return true;
            }
            catch (Exception xyz)
            {
                if (transaction != null) transaction.Dispose();
                transaction = null;
                transactionLock = false;
                return false;
            }
        }

        public Boolean Rollback()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                    transaction = null;
                    transactionLock = false;
                }
                return true;
            }
            catch (Exception xyz)
            {
                return false;
            }
        }

        public String getDatabaseDirectory()
        {
            return null;
        }

        public Boolean GetTransactionLockStatus()
        {
            return transactionLock;
        }

        public DataTable ExecuteDynamic(string sql, Tree indyn)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlCommand mycommand = new SqlCommand();
                int indyntreeCount = indyn.tree.Count;
                for (int index = 0; index < indyntreeCount; index++)
                {
                    string key = (string)indyn.leafnames[index];
                    string value = indyn.GetElement(key);
                    mycommand.Parameters.Add(new SqlParameter(key, value));
                }
                mycommand.Connection = dbCursor;
                mycommand.CommandText = sql;
                if (transaction!=null)
                {
                    mycommand.Transaction = transaction;
                }
                SqlDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                mycommand.Dispose();
            }

            catch (Exception e)
            {
                System.Console.Out.WriteLine(e.Message);
                System.Console.Out.WriteLine(e.StackTrace);
                throw new Exception(e.Message);
            }
            return dt;
        }

        public void setDatabaseSoftware(int softwaretype)
        {
            SoftwareType = softwaretype;
        }
        public int getDatabaseSoftware()
        {
            return SoftwareType;
        }
        public void setDatabaseRevision(int softwarerevision)
        {
            SoftwareRevision = softwarerevision;
        }
        public int getDatabaseRevision()
        {
            return SoftwareRevision;
        }

        public bool CreateDatabase(String database)
        {
            Execute("CREATE DATABASE " + database + ";");
            return true;
        }

        public bool DropDatabase(String database)
        {
            Execute("DROP DATABASE " + database + ";");
            return true;
        }

        public bool CheckDatabaseExists(String database)
        {
            bool result;
            string DatabaseCheckString = "select name from master.sys.databases where name='"+database+"';";
            SqlConnection MyConn = new SqlConnection(ConnectionString);
            SqlCommand DatabaseExistsCommand = new SqlCommand(DatabaseCheckString, MyConn);
            MyConn.Open();
            SqlDataReader Reader = DatabaseExistsCommand.ExecuteReader();
            result = Reader.HasRows;
            Reader.Close();
            MyConn.Close();
            return result;
        }

        protected string prebuiltMessageString = "insert into [documents]([Received], [Label], [Category], [Metadata], [UniqueID], [Document]) values (@Received, @Label, @Category, @Metadata, @UniqueID, @Document);";

        public bool InsertPreparedDocument(string[] data)
        {
            Boolean returnCode = true;

            try
            {
                SqlCommand mycommand = new SqlCommand();

                mycommand.Parameters.AddWithValue("@Received", long.Parse(data[0]));
                mycommand.Parameters.AddWithValue("@Label", data[1]);
                mycommand.Parameters.AddWithValue("@Category", data[2]);
                mycommand.Parameters.AddWithValue("@Metadata", data[3]);
                mycommand.Parameters.AddWithValue("@Message", data[4]);
                mycommand.Parameters.AddWithValue("@UniqueID", data[5]);

                mycommand.Connection = dbCursor;
                mycommand.CommandText = prebuiltMessageString;
                if (transaction != null)
                {
                    mycommand.Transaction = transaction;
                }
                mycommand.Prepare();
                mycommand.ExecuteNonQuery();
                mycommand.Dispose();
            }
            catch (Exception xyz)
            {
                returnCode = false;
            }
            return returnCode;
        }

        public DataTable ExecuteDynamicWithFile(string sql, Tree indyn, string fileField, Byte[] byteData)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlCommand mycommand = new SqlCommand();
                int indyntreeCount = indyn.tree.Count;
                for (int index = 0; index < indyntreeCount; index++)
                {
                    string key = (string)indyn.leafnames[index];
                    string value = indyn.GetElement(key);
                    mycommand.Parameters.Add(new SqlParameter(key, value));
                }

                mycommand.Parameters.Add(new SqlParameter(fileField, byteData));

                mycommand.Connection = dbCursor;
                mycommand.CommandText = sql;
                if (transaction != null)
                {
                    mycommand.Transaction = transaction;
                }
                SqlDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                mycommand.Dispose();
            }

            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return dt;
        }
    }
}
