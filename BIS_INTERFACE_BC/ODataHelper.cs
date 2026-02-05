using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BIS_INTERFACE_BC
{
    class ODataHelper
    {

        public async Task<DataTable> FetchAllODataAsync(string firstUrl, string username, string password)
        {
            var dtAll = new DataTable("OData");
            try
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };


                bool firstPage = true;

                using (var client = new HttpClient(handler))
                {
                    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.Timeout = TimeSpan.FromSeconds(120);

                    string nextUrl = firstUrl;

                    while (!string.IsNullOrEmpty(nextUrl))
                    {
                        var resp = await client.GetAsync(nextUrl);
                        resp.EnsureSuccessStatusCode();

                        string json = await resp.Content.ReadAsStringAsync();

                        // *** กันเคสที่ json ไม่ใช่ JSON จริง ๆ ***
                        if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{')
                        {
                            throw new Exception(
                                "Response ไม่ใช่ JSON (หน้า " + nextUrl + "). ตัวอย่าง: " +
                                json.Substring(0, Math.Min(300, json.Length)));
                        }

                        // แปลงหน้า current → DataTable (ใช้ฟังก์ชันเดิมของคุณ)
                        DataTable dtPage = await ODataJsonToDataTableAsync(json);

                        if (firstPage)
                        {
                            dtAll = dtPage.Clone();
                            firstPage = false;
                        }

                        foreach (DataRow r in dtPage.Rows)
                            dtAll.ImportRow(r);

                        // ====== หา nextLink ======
                        var root = JObject.Parse(json);

                        // ลองทั้งแบบ V2 (__next) และ V4 (@odata.nextLink)
                        string nextLink =
                            (string)root.SelectToken("d.__next") ??
                            (string)root.SelectToken("@odata.nextLink") ??
                            (string)root.SelectToken("odata.nextLink"); // กันบาง service ใช้ชื่อแบบนี้

                        if (!string.IsNullOrEmpty(nextLink))
                        {
                            // ถ้า SAP ส่งมาเป็น relative path ต้องแปลงเป็น absolute
                            if (!nextLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                var baseUri = new Uri(firstUrl);
                                nextLink = new Uri(baseUri, nextLink).ToString();
                            }

                            nextUrl = nextLink;
                        }
                        else
                        {
                            // ไม่มี nextLink แล้ว → จบ loop
                            nextUrl = null;
                        }
                    }
                }

                return dtAll;
            }
            catch (Exception e)
            {
                return dtAll;
            }

        }


        public async Task<DataTable> FetchODataSinglePageAsync(string url, string username, string password)
        {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using (var client = new HttpClient(handler))
            {
                var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authValue);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(120);

                // *** จุดที่คุณค้างอยู่ ***
                var resp = await client.GetAsync(url);

                // ดู status code ก่อน
                var status = resp.StatusCode;
                string body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    // โยน error พร้อมเนื้อหาที่ SAP ตอบกลับมา
                    throw new Exception(
                        $"HTTP {(int)status} {status} from SAP\nURL: {url}\nBody:\n{body}");
                }

                // ถ้า response ไม่ใช่ JSON ให้ error ชัด ๆ
                if (string.IsNullOrWhiteSpace(body) || body.TrimStart()[0] != '{')
                {
                    throw new Exception("Response ไม่ใช่ JSON: " +
                                        body.Substring(0, Math.Min(200, body.Length)));
                }

                return ODataJsonToDataTable(body);
            }
        }

 
        private DataTable ODataJsonToDataTable(string json)
        {
            JObject root = JObject.Parse(json);

            // พยายามหาผลลัพธ์ในหลายรูปแบบ
            JToken arr =
                root.SelectToken("d.results") ??   // OData V2 แบบ list
                root.SelectToken("value") ??       // OData V4 แบบ list
                root.SelectToken("d");             // OData V2 แบบ single object

            if (arr == null)
                throw new Exception("ไม่พบผลลัพธ์ใน JSON (ไม่มี d.results / value / d)");

            // ถ้าได้ Object เดี่ยว ให้ห่อเป็น Array 1 ตัว
            if (arr.Type == JTokenType.Object)
            {
                arr = new JArray(arr);
            }

            if (arr.Type != JTokenType.Array)
                throw new Exception("โครงสร้างผลลัพธ์ไม่ใช่ Array");

            var dt = new DataTable("OData");

            // สร้างคอลัมน์
            foreach (JObject jo in arr)
            {
                foreach (var p in jo.Properties())
                {
                    if (p.Value.Type != JTokenType.Object && p.Value.Type != JTokenType.Array)
                    {
                        if (!dt.Columns.Contains(p.Name))
                            dt.Columns.Add(p.Name, typeof(string));
                    }
                }
            }

            // เติม Row
            foreach (JObject jo in arr)
            {
                var row = dt.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    JToken tok = jo.SelectToken(col.ColumnName);
                    if (tok != null && tok.Type != JTokenType.Object && tok.Type != JTokenType.Array)
                        row[col.ColumnName] = tok.ToString();
                    else
                        row[col.ColumnName] = DBNull.Value;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }


        // รองรับทั้งรูปแบบ {"d":{"results":[...]}} และ {"value":[...]} (บาง service)
        private async Task<DataTable> ODataJsonToDataTableAsync(string json)
        {
            JObject root = JObject.Parse(json);

            JToken arr = null;
            if (root.SelectToken("d.results") != null)
            {
                arr = root.SelectToken("d.results");
            }
            else if (root.SelectToken("value") != null)
            {
                arr = root.SelectToken("value");
            }

            if (arr == null || arr.Type != JTokenType.Array)
            {
                throw new Exception("ไม่พบรายการผลลัพธ์ใน JSON (ไม่มี d.results หรือ value)");
            }

            var dt = new DataTable("OData");

            // รวมคอลัมน์จากทุกแอตทริบิวต์ระดับบน (ข้าม object/array ซ้อน)
            foreach (JObject jo in arr)
            {
                foreach (var p in jo.Properties())
                {
                    if (p.Value.Type != JTokenType.Object && p.Value.Type != JTokenType.Array)
                    {
                        if (!dt.Columns.Contains(p.Name))
                        {
                            dt.Columns.Add(p.Name, typeof(string));
                        }
                    }
                }
            }

            // เติมข้อมูลลง DataTable
            foreach (JObject jo in arr)
            {
                var row = dt.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    JToken tok = jo.SelectToken(col.ColumnName);
                    if (tok != null && tok.Type != JTokenType.Object && tok.Type != JTokenType.Array)
                    {
                        row[col.ColumnName] = tok.ToString();
                    }
                    else
                    {
                        row[col.ColumnName] = DBNull.Value; // หรือจะใช้ null ก็ได้ถ้าไม่ผูกกับ DB ตรง ๆ
                    }
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
