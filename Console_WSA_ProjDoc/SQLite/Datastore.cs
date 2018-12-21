using Console_WSA_ProjDoc.General;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_WSA_ProjDoc.SQLite
{
    class Datastore
    {
        private static SQLiteConnection sqliteConnection;
        public static string dbSource;

        public Datastore(string dbsource) => dbSource = dbsource;
       
        public class History
        {
            public string File { get; set; }
            public int Id { get; set; }
            public DateTime CreationDate { get; set; }
            public string Creator { get; set; }
            public string Comment { get; set; }
        }

        private static SQLiteConnection DbConnection()
        {
            sqliteConnection = new SQLiteConnection("Data Source=" + dbSource + "; Version=3;");
            sqliteConnection.Open();
            return sqliteConnection;
        }

        public void DbCreation()
        {
            try
            {
                SQLiteConnection.CreateFile(dbSource);
                CreateHistoryTable();
            }
            catch(Exception e)
            {
                Logging.WriteLog("An Exception occurred during the SQLite Database Creation: " + e.Message);
                throw e;
            }
        }
        public static void CreateHistoryTable()
        {
            try
            {
                using (var cmd = DbConnection().CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE History(" +
                        "codefile VARCHAR(500)," +
                        "id INT," +
                        "creationdate SMALLDATETIME," +
                        "creator VARCHAR(500)," +
                        "comment VARCHAR(500))";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog("An Exception occurred during the SQLite Table Creation: " + e.Message);
                throw e;
            }
        }

        public DataTable QueryRecord(string file)
        {
            SQLiteDataAdapter da = null;
            DataTable dt = new DataTable();
            try
            {
                using (var cmd = DbConnection().CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM History Where codefile='" + file + "'";
                    da = new SQLiteDataAdapter(cmd.CommandText, DbConnection());
                    da.Fill(dt);

                    return dt;
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog("An Exception occurred during the SQLite Data Selection: " + e.Message);
                throw e;
            }
        }

        public void AddRecord(History History)
        {
            try
            {
                using (var cmd = DbConnection().CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO History(codefile, id, creationdate, creator, comment) " +
                        "values (@codefile, @id, @creationdate, @creator, @comment)";
                    cmd.Parameters.Add(new SQLiteParameter("@codefile", History.File));
                    cmd.Parameters.Add(new SQLiteParameter("@id", History.Id));
                    cmd.Parameters.Add(new SQLiteParameter("@creationdate", History.CreationDate));
                    cmd.Parameters.Add(new SQLiteParameter("@creator", History.Creator));
                    cmd.Parameters.Add(new SQLiteParameter("@comment", History.Comment));

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog("An Exception occurred during the SQLite Data Insert: " + e.Message);
                throw e;
            }
        }
    }
}
