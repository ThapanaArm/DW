using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Globalization;

namespace BIS_INTERFACE_BC
{
    class SQLConnect
    {
        public static SqlConnection GetConnectionDW()
        {

            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbBC"].ConnectionString);
            con.Open();
            return con;
        }

        public static string GetStringValue(string sqlCommand, string DB)
        {
            string connStr = "";

            if (DB == "dbDW")
            {
                connStr = ConfigurationManager.ConnectionStrings["dbDW"].ConnectionString;
            }

            else
            {
                throw new Exception("ไม่พบชื่อ DB");
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sqlCommand, con))
                {
                    con.Open();

                    object result = cmd.ExecuteScalar();

                    return result?.ToString() ?? "";
                }
            }
        }

        public static DataTable GetDataTable(String sqlCommand, String DB)
        {

            DataTable dataTable = new DataTable();
            string str = "";

            if (DB == "dbDW")
            {
                str = ConfigurationManager.ConnectionStrings["dbDW"].ConnectionString;
            }


            SqlConnection con = new System.Data.SqlClient.SqlConnection();
            con.ConnectionString = str;

            using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand, con))
            {

                adapter.Fill(dataTable);
                con.Close();

            }
            return dataTable;
        }
        public static string Updatedata(String query, String DB)
        {

            try
            {
                string str = "";
                if (DB == "dbDW")
                {
                    str = ConfigurationManager.ConnectionStrings["dbDW"].ConnectionString;
                }

                SqlConnection con = new System.Data.SqlClient.SqlConnection();
                con.ConnectionString = str;

                using (SqlConnection connection = new SqlConnection(str))
                    if (query == "")
                    {
                        return "";
                    }

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandTimeout = 120;
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                    con.Close();

                    return "";
                }

            }
            catch (Exception e)
            {
                string errorMessage = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,QuerySystax,LogDescription) VALUES ('Update Data','Fail',getdate(),'{query}','{errorMessage}');", "dbDW");
                Console.WriteLine(e.Message);
                return "";
            }


        }
    }


}
