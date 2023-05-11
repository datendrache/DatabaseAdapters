//   Database Adapters
//   Copyright (C) 2003-2019 Eric Knight

using System.Data;
using FatumCore;
using System.Data.SQLite;

namespace DatabaseAdapters
{
    public class SQLiteDatabase : IntDatabase
    {
        String dbConnection;
        public SQLiteConnection dbCursor;
        public string ConnectionString = "";
        private SQLiteTransaction transaction = null;
        private Boolean transactionLock = false;
        int SoftwareType = 2;
        int SoftwareRevision = 0;

        // SQLite Only Variables

        private string DatabaseFilename = "";

        /// <summary>
        ///     Default Constructor for SQLiteDatabase Class.
        /// </summary>

        public SQLiteDatabase(String inputFile)
        {
            DatabaseFilename = inputFile;

            if (File.Exists(inputFile))
            {
                dbConnection = String.Format("Data Source={0}", inputFile);
                dbCursor = new SQLiteConnection(dbConnection);
                
            }
            else
            {
                FileInfo fi = new FileInfo(inputFile);

                if (!Directory.Exists(fi.DirectoryName))
                {
                    Directory.CreateDirectory(fi.DirectoryName);
                }

                SQLiteConnection.CreateFile(inputFile);
                dbConnection = String.Format("Data Source={0}", inputFile);
                dbCursor = new SQLiteConnection(dbConnection);
            }
            dbCursor.Open();
            ExecuteNonQuery("PRAGMA synchronous = OFF;");
            ExecuteNonQuery("PRAGMA journal_mode = OFF;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
            ExecuteNonQuery("PRAGMA page_size = 4096;");

            //dbCursor.sqlite3_exec(db, , NULL, NULL, &sErrMsg);
        }

        public int getDatabaseType()
        {
            return DatabaseSoftware.SQLite;
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
                dbCursor.Dispose();
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

        public SQLiteDatabase(Dictionary<String, String> connectionOpts)
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
                SQLiteCommand mycommand = new SQLiteCommand(dbCursor);
                mycommand.CommandText = sql;
                mycommand.Prepare();
                SQLiteDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
            }

            catch (Exception e)
            {
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
            SQLiteCommand mycommand = new SQLiteCommand(dbCursor);
            mycommand.CommandText = sql;
            mycommand.Prepare();
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
            SQLiteCommand mycommand = new SQLiteCommand(dbCursor);
            mycommand.CommandText = sql;
            mycommand.Prepare();
            object value = mycommand.ExecuteScalar();
            return value;
        }

        public object ExecuteScalarTree(string sql, Tree data)
        {
            SQLiteCommand mycommand = new SQLiteCommand(dbCursor);
            mycommand.CommandText = sql;
            int indyntreeCount = data.tree.Count;
            for (int index = 0; index < indyntreeCount; index++)
            {
                string key = (string)data.leafnames[index];
                string value = data.getElement(key);
                mycommand.Parameters.AddWithValue(key, value);
            }

            if (transaction != null)
            {
                mycommand.Transaction = transaction;
            }
            object result = mycommand.ExecuteScalar();
            return result;
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
                string value = data.getElement(key);

                if (key.Substring(0, 1) != "_")
                {
                    if (key.Substring(0, 1) != "*")
                    {
                        Tree casting = data.findNode("_" + key);

                        if (casting != null)  // This key is typecast
                        {

                            switch (casting.Value.ToLower())
                            {
                                case "bigint":
                                case "integer":
                                case "smallint":
                                    values += String.Format(" [{0}]=CAST({1} as INTEGER),", key, "@value" + i.ToString());
                                    parms.addElement("@value" + i.ToString(), value);
                                    break;
                                case "float":
                                case "real":
                                    values += String.Format(" [{0}]=CAST({1} as REAL),", key, "@value" + i.ToString());
                                    parms.addElement("@value" + i.ToString(), value);
                                    break;
                                default:
                                    values += String.Format(" [{0}]={1},", key, "@value" + i.ToString());
                                    parms.addElement("@value" + i.ToString(), value);
                                    break;
                            }
                        }
                        else
                        {
                            values += String.Format(" [{0}]={1},", key, "@value" + i.ToString());
                            parms.addElement("@value" + i.ToString(), value);
                        }
                    }   
                    else
                    {
                        parms.addElement(key.Substring(1), value);
                    }
                }
            }
            values = values.Substring(0, values.Length - 1);

            try
            {
                if (where != "")
                {
                    this.ExecuteDynamic(String.Format("update {0} set {1} where {2};", tableName, values, where), parms);
                }
                else
                {
                    this.ExecuteDynamic(String.Format("update {0} set {1};", tableName, values), parms);
                }
            }
            catch (Exception)
            {
                //MessageBox.Show(fail.Message);

                returnCode = false;
            }
            parms.dispose();
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
        ///     Allows the programmer to easily delete rows from the DB.
        /// </summary>
        /// <param name="tableName">The table from which to delete.</param>
        /// <param name="where">The where clause for the delete.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>

        //public bool Delete(String tableName, String where)
        //{
        //    Boolean returnCode = true;

        //    try
        //    {
        //        this.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));
        //    }

        //    catch (Exception fail)
        //    {
        //        returnCode = false;
        //    }

        //    return returnCode;
        //}


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
                string key = (string)data.leafnames[i];
                string value = data.getElement(key);

                if (key.Substring(0, 1) != "_")
                {
                    if (key.Substring(0, 1) != "*")
                    {
                        Tree casting = data.findNode("_" + key);

                        if (casting != null)  // This key is typecast
                        {
                            columns += String.Format(" [{0}],", key);

                            switch (casting.Value.ToLower())
                            {
                                case "bigint":
                                case "integer":
                                case "smallint":
                                    values += " CAST(@value" + i.ToString() + " as INTEGER),";
                                    break;
                                case "float":
                                case "real":
                                    values += " CAST(@value" + i.ToString() + " as REAL),";
                                    break;
                                default:
                                    values += " @value" + i.ToString() + ",";
                                    break;
                            }
                            parms.addElement("@value" + i.ToString(), value);
                        }
                        else
                        {
                            columns += String.Format(" [{0}],", key);
                            values += " @value" + i.ToString() + ",";
                            parms.addElement("@value" + i.ToString(), value);
                        }
                    }
                    else
                    {
                        parms.addElement(key.Substring(1), value);
                    }
                }
            }

            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);

            try
            {
                this.ExecuteDynamic(String.Format("insert into {0}({1}) values({2});", tableName, columns, values), parms);
            }
            catch (Exception xyz)
            {
                returnCode = false;
            }
            parms.dispose();
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily delete all data from the DB.
        /// </summary>
        /// <returns>A boolean true or false to signify success or failure.</returns>

        //public bool ClearDB()
        //{
        //    DataTable tables;

        //    try
        //    {
        //        tables = this.Execute("select NAME from SQLITE_MASTER where type='table' order by NAME;");
        //        foreach (DataRow table in tables.Rows)
        //        {
        //            this.ClearTable(table["NAME"].ToString());
        //        }

        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

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
                if (transaction != null) transaction.Dispose();
                transaction = null;
                transactionLock = false;
                return false;
            }
            return result;
        }

        public Boolean Commit()
        {
            try
            {
                if (transaction != null)
                {
                    lock (transaction)
                    {
                        transaction.Commit();
                        transaction.Dispose();
                        transaction = null;
                        transactionLock = false;
                    }
                }
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
                    lock (transaction)
                    {
                        transaction.Rollback();
                        transactionLock = false;
                        transaction = null;
                    }
                }
                return true;
            }
            catch (Exception xyz)
            {
                transactionLock = false;
                transaction = null;
                return false;
            }
        }

        public String getDatabaseDirectory()
        {
            FileInfo fi = new FileInfo(DatabaseFilename);
            return fi.DirectoryName;
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
                SQLiteCommand mycommand = new SQLiteCommand();
                int indyntreeCount = indyn.tree.Count;
                for (int index = 0; index < indyntreeCount; index++)
                {
                    string key = (string)indyn.leafnames[index];
                    string value = indyn.getElement(key);
                    mycommand.Parameters.AddWithValue(key, value);
                }
                mycommand.Connection = dbCursor;
                mycommand.CommandText = sql;
                mycommand.Prepare();
                SQLiteDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
            }

            catch (Exception e)
            {
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
            // All SQLite Databases are created at the time of allocation of this interface
            return true;
        }

        public bool DropDatabase(String database)
        {
            // All SQLite Databases are created at the time of allocation of this interface
            return true;
        }

        public bool CheckDatabaseExists(String database)
        {
            // All SQLite Databases are created at the time of allocation of this interface
            return true;
        }

        protected string prebuiltMessageString = "insert into [documents]([Received], [Label], [Category], [Metadata], [ID], [Document]) values (@Received, @Label, @Category, @Metadata, @ID, @Document);";

        public bool InsertPreparedDocument(String[] data)
        {
            Boolean returnCode = true;

            try
            {
                SQLiteCommand mycommand = new SQLiteCommand();

                mycommand.Parameters.Add("@Received", DbType.Int64).Value = long.Parse(data[0]);
                mycommand.Parameters.Add("@Label", DbType.String).Value = data[1];
                mycommand.Parameters.Add("@Category", DbType.String).Value = data[2];
                mycommand.Parameters.Add("@Metadata", DbType.String).Value = data[3];
                mycommand.Parameters.Add("@ID", DbType.Int64).Value = data[4];
                mycommand.Parameters.Add("@Document", DbType.String).Value = data[5];

                mycommand.Connection = dbCursor;
                mycommand.CommandText = prebuiltMessageString;
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
                SQLiteCommand mycommand = new SQLiteCommand();
                int indyntreeCount = indyn.tree.Count;
                for (int index = 0; index < indyntreeCount; index++)
                {
                    string key = (string)indyn.leafnames[index];
                    string value = indyn.getElement(key);
                    mycommand.Parameters.AddWithValue(key, value);
                }

                mycommand.Parameters.AddWithValue(fileField, byteData);

                mycommand.Connection = dbCursor;
                mycommand.CommandText = sql;
                mycommand.Prepare();
                SQLiteDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
            }

            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return dt;
        }
    }
}
