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

        // ต่างจาก Updatedata ตรงที่โยน exception ออกไปเมื่อ error และคืนจำนวนแถวที่ได้รับผล //--Sukanya 031169
        public static int ExecuteStrict(string query, string DB)
        {
            if (DB != "dbDW")
                throw new Exception("ไม่พบชื่อ DB");

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbDW"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandTimeout = 120;
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // ===== แก้ทั้งฟังก์ชัน Updatedata //--Sukanya 061169 11:27 =====
        // ปัญหาเดิม: (1) catch เรียก Updatedata ตัวเองเพื่อ log โดยไม่ escape ' ใน query -> log พังซ้ำ -> เรียกตัวเองวนไม่จบ
        //            (2) ถ้า ExecuteNonQuery error, connection ไม่ถูกปิด -> รั่วจน pool เต็ม -> ค้างที่ Open() นานตาม Connect Timeout (60000 วินาที)
        // เจอทั้งเครื่อง debug (Purchase วันที่ dd/MM) และ HP-SAP-RPA (SalesOrder มี ' ใน PO ลูกค้า) วันที่ 061169
        public static string Updatedata(String query, String DB)
        {
            if (string.IsNullOrEmpty(query)) return ""; //--Sukanya 061169 11:27 เช็กก่อนเปิด connection

            try
            {
                string str = "";
                if (DB == "dbDW")
                {
                    str = ConfigurationManager.ConnectionStrings["dbDW"].ConnectionString;
                }

                using (SqlConnection con = new SqlConnection(str))   //--Sukanya 061169 11:27 ใช้ using ให้ปิด connection เสมอแม้ error
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandTimeout = 120;
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                LogUpdateFail(query, e.Message); //--Sukanya 061169 11:27 log ครั้งเดียว ไม่เรียก Updatedata ซ้ำ
                return "";
            }
        }

        // เขียน Log_Status ของ Updatedata ที่ error แบบ parameter (ไม่ต้อง escape ') และไม่ recursive //--Sukanya 061169 11:27
        // ตัดความยาว query/ข้อความตามขนาดคอลัมน์จริง ถ้ายัง log ไม่ได้ให้พิมพ์ที่ Console แทน
        private static void LogUpdateFail(string query, string message)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbDW"].ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO Log_Status(Process,Status,LogDate,QuerySystax,LogDescription)
                    VALUES ('Update Data','Fail',GETDATE(),
                        LEFT(@query, CASE WHEN COL_LENGTH('Log_Status','QuerySystax') = -1 THEN 1000000 ELSE COL_LENGTH('Log_Status','QuerySystax') / 2 END),
                        LEFT(@message, CASE WHEN COL_LENGTH('Log_Status','LogDescription') = -1 THEN 1000000 ELSE COL_LENGTH('Log_Status','LogDescription') / 2 END))", con))
                {
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.Add("@query", SqlDbType.NVarChar, -1).Value = query ?? "";
                    cmd.Parameters.Add("@message", SqlDbType.NVarChar, -1).Value = message ?? "";
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Data : เขียน Log_Status ไม่ได้ ({ex.Message})");
            }
        }
    }


}
