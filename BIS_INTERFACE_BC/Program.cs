using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;


namespace BIS_INTERFACE_BC
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        private static string _USERNAME = "ABAPUSER";
        private static string _PASSWORD = "Thailand@12345678901234567890";
        static async Task Main()
        { // รันงาน sync แบบ block ให้จบก่อน

            ////Product
            //await Product("Product");
            //await ProductDescription("ProductDescription");
            //await ProductPlant("ProductPlant");
            //await ProductPlantProcurement("ProductPlantProcurement"); //ปรับถึงนี้
            //await ProductSalesDelivery("ProductPlantProcurement");
            //await ProductSalesTax("ProductSalesTax");
            //await ProductStorage("ProductStorage");

            ////Business Partner
            //await BusinessPartner("BusinessPartner");
            //await BusinessPartnerAddress("BusinessPartnerAddress");
            //await BusinessPartnerEmail("BusinessPartnerEmail");
            //await BusinessPartnerPhone("BusinessPartnerPhone");
            //await BusinnessPartnerCustomer("BusinnessPartnerCustomer");
            //await BusinnessPartnerCustomerCompany("BusinnessPartnerCustomerCompany");
            //await BusinnessPartnerCustomerSalesaArea("BusinnessPartnerCustomerSalesaArea");
            //await BusinessPartnerCustSalesPartnerFunc("BusinessPartnerCustSalesPartnerFunc");

            ////Sales Order
            //await AddSalesOrder("SalesOrder");
            //await UpdateSalesOrder("SalesOrder");
            //await RecheckcSalesOrderHeaderPartner("Recheck");


            ////OutbDelivery
            //await AddOutbDelivery("OutbDelivery");
            ////Billing
            //await AddBilling("Billing");
            //await UpdateBilling("Billing");





            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            if (Environment.UserInteractive)
            {
                // ตรงนี้ก็ต้องเปลี่ยนเป็น Service1 ด้วย
                Service1 service = new Service1();

                // หมายเหตุ: คุณต้องไปสร้าง Method ชื่อ OnStartExternal ใน Service1.cs ก่อน 
                // หรือจะใช้ Reflection เรียก OnStart ตรงๆ ก็ได้เพื่อความรวดเร็วในการ Debug
                typeof(ServiceBase).GetMethod("OnStart",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(service, new object[] { new string[] { } });

                Console.WriteLine("Service is running... Press any key to stop.");
                Console.ReadKey();


                typeof(ServiceBase).GetMethod("OnStop",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(service, null);
                // จบโปรแกรม
                Environment.Exit(0);
            }
            else
            {
                ServiceBase.Run(ServicesToRun);
            }

        }
        private static string EscapeSql(object input)
        {
            if (input == null || input == DBNull.Value) return "";
            return input.ToString().Replace("'", "''").Trim();
        }
        //Product Group
        private static async Task Product(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            try
            {
                var helper = new ODataHelper();
                string url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'Product' and Isactive = 1 and DataSource = 'OData'", "dbDW");
                DataTable dt = await helper.FetchAllODataAsync(url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    string product = row["Product"].ToString().Replace("'", "''");
                    // 1. ระบุ Culture เป็นไทย เพื่อรองรับปี 2568
                    CultureInfo thaiCulture = new CultureInfo("th-TH");

                    // 2. ดึงค่าจาก row ออกมาเป็น string
                    string rawDate = row["CreationDate"].ToString();

                    string formattedCreation;
                    string formattedLastChange;
                    // 3. ใช้ TryParseExact เพื่อป้องกัน Error หาก String มี Format แปลกๆ
                    if (DateTime.TryParseExact(rawDate, "d/M/yyyy H:mm:ss", thaiCulture, DateTimeStyles.None, out DateTime tempDate))
                    {
                        // แปลงเป็น ค.ศ. และ Format ที่ต้องการ
                        formattedCreation = tempDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // กรณี Parse ไม่ผ่าน (เช่น ข้อมูลว่าง หรือ Format ไม่ตรงกับ d/M/yyyy H:mm:ss)
                        formattedCreation = "1900-01-01"; // หรือค่า Default อื่นๆ
                    }



                    string rawDateLastChange = row["LastChangeDate"].ToString();

                    // 3. ใช้ TryParseExact เพื่อป้องกัน Error หาก String มี Format แปลกๆ
                    if (DateTime.TryParseExact(rawDateLastChange, "d/M/yyyy H:mm:ss", thaiCulture, DateTimeStyles.None, out DateTime tempLastDate))
                    {
                        // แปลงเป็น ค.ศ. และ Format ที่ต้องการ
                        formattedLastChange = tempLastDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // กรณี Parse ไม่ผ่าน (เช่น ข้อมูลว่าง หรือ Format ไม่ตรงกับ d/M/yyyy H:mm:ss)
                        formattedLastChange = "1900-01-01"; // หรือค่า Default อื่นๆ
                    }

                    // ใช้ SQL Logic จัดการ Insert/Update ในคำสั่งเดียว
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_Product WHERE Product = '{product}')
                BEGIN
                    UPDATE Ms_Product SET 
                        ProductGroup = '{row["ProductGroup"]}', ProductType = '{row["ProductType"]}',
                        LastChangeDate = '{formattedLastChange}', GrossWeight = '{row["GrossWeight"]}',
                        NetWeight = '{row["NetWeight"]}', BaseUnit = '{row["BaseUnit"]}',
                        HSCODE_PRD = '{row["YY1_HSCODE_PRD"]}'
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_Product (Product, ProductGroup, ProductType, CreationDate, LastChangeDate, GrossWeight, NetWeight, BaseUnit, HSCODE_PRD)
                    VALUES ('{product}', '{row["ProductGroup"]}', '{row["ProductType"]}', '{formattedCreation}', '{formattedLastChange}', '{row["GrossWeight"]}', '{row["NetWeight"]}', '{row["BaseUnit"]}', '{row["YY1_HSCODE_PRD"]}');
                END");

                    rowCount++;
                    if (rowCount % 100 == 0) // ทุกๆ 100 แถว ให้ยิงเข้า DB ทีเดียว
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }
                if (batchSql.Length > 0) SQLConnect.Updatedata(batchSql.ToString(), "dbDW");

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('Product','Successful',getdate());", "dbDW");
                Console.WriteLine($"Product : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('Product','Fail',getdate(),'{e.Message.Replace("'", "''")}');", "dbDW");
            }
        }
        private static async Task ProductDescription(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL มาครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductDescription' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. ดึงข้อมูลจาก OData ทั้งหมดมาไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow rowquery in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเพื่อป้องกัน SQL Injection และ Error จาก Single Quote
                    string product = rowquery["Product"].ToString().Replace("'", "''");
                    string description = rowquery["ProductDescription"].ToString().Replace("'", "''").Replace(",", "");
                    string language = rowquery["Language"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL Logic เพื่อตัดสินใจ Insert หรือ Update ที่ฝั่ง Server เลย (ลดการ Query Check จาก C#)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_ProductDescription WHERE Product = '{product}')
                BEGIN
                    UPDATE Ms_ProductDescription SET 
                        ProductDescription = '{description}', 
                        Language = '{language}', 
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_ProductDescription (Product, ProductDescription, Language, UpdateDate)
                    VALUES ('{product}', '{description}', '{language}', GETDATE());
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลเข้า DB เป็นชุด (Batch) ชุดละ 100 แถว เพื่อความเร็วสูงสุด
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // ส่งข้อมูลชุดสุดท้ายที่เหลือ
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log สำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductDescription','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductDescription : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errorMessage = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductDescription','Fail',getdate(),'{errorMessage}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }
        private static async Task ProductPlant(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การดึงข้อมูลจาก Setting ครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductPlant' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. Fetch ข้อมูลทั้งหมดจาก OData มาไว้ที่ DataTable (ดึงมาทีเดียว)
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเบื้องต้น
                    string product = row["Product"].ToString().Replace("'", "''");
                    string plant = row["Plant"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL IF EXISTS เพื่อจัดการทั้ง Insert และ Update ในคำสั่งเดียว (ลดการ Round-trip)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM MS_ProductPlant WHERE Product = '{product}')
                BEGIN
                    UPDATE MS_ProductPlant SET 
                        Plant = '{plant}',
                        PurchasingGroup = '{row["PurchasingGroup"]}',
                        CountryOfOrigin = '{row["CountryOfOrigin"]}',
                        AvailabilityChecktype = '{row["AvailabilityChecktype"]}',
                        PeriodType = '{row["PeriodType"]}',
                        ProfitCenter = '{row["ProfitCenter"]}',
                        MaintenanceStatusName = '{row["MaintenanceStatusName"]}',
                        MRPType = '{row["MRPType"]}',
                        MinimumLotSizeQuantity = '{row["MinimumLotSizeQuantity"]}',
                        MaximumLotSizeQuantity = '{row["MaximumLotSizeQuantity"]}',
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO MS_ProductPlant (
                        Product, Plant, PurchasingGroup, CountryOfOrigin, AvailabilityChecktype, 
                        PeriodType, ProfitCenter, MaintenanceStatusName, MRPType, 
                        MinimumLotSizeQuantity, MaximumLotSizeQuantity, UpdateDate
                    ) VALUES (
                        '{product}', '{plant}', '{row["PurchasingGroup"]}', '{row["CountryOfOrigin"]}', '{row["AvailabilityChecktype"]}', 
                        '{row["PeriodType"]}', '{row["ProfitCenter"]}', '{row["MaintenanceStatusName"]}', '{row["MRPType"]}', 
                        '{row["MinimumLotSizeQuantity"]}', '{row["MaximumLotSizeQuantity"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลเข้า DB เป็นชุด (Batch) ชุดละ 100 แถว เพื่อลด overhead ของการเชื่อมต่อ
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // ส่งข้อมูลที่เหลือในชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log เมื่อทำงานสำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductPlant','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductPlant : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductPlant','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }
        private static async Task ProductPlantProcurement(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductPlantProcurement' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. ดึงข้อมูล OData ทั้งหมดมาพักไว้ใน DataTable (High Performance Fetch)
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    string product = row["Product"].ToString().Replace("'", "''");
                    string plant = row["Plant"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL "UPSERT" Logic (IF EXISTS) เพื่อรวมคำสั่งเช็คและเขียนข้อมูลในจังหวะเดียว
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_ProductPlantProcurement WHERE Product = '{product}')
                BEGIN
                    UPDATE Ms_ProductPlantProcurement SET 
                        Plant = '{plant}', 
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_ProductPlantProcurement (Product, Plant, UpdateDate)
                    VALUES ('{product}', '{plant}', GETDATE());
                END");

                    rowCount++;

                    // 4. ส่งคำสั่งไปประมวลผลที่ SQL Server เป็นชุด (Batch size = 100)
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกข้อมูลชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึกสถานะความสำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductPlantProcurement','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductPlantProcurement : Sync Successful ({rowCount} rows)");

            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductPlantProcurement','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }
        private static async Task ProductSalesDelivery(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียวจาก DB
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductSalesDelivery' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. ดึงข้อมูลจาก SAP OData ทั้งหมดมาพักไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเพื่อป้องกัน SQL Error
                    string product = row["Product"].ToString().Replace("'", "''");

                    // 3. สร้าง SQL Logic แบบ "ถ้ามีให้ Update ถ้าไม่มีให้ Insert" (UPSERT)
                    // หมายเหตุ: มีการ Map ชื่อ Field จาก OData (FirstSalesSpec...) ไปยังตาราง (MaterialGroup...)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_ProductSalesDelivery WHERE Product = '{product}')
                BEGIN
                    UPDATE Ms_ProductSalesDelivery SET 
                        ProductSalesOrg = '{row["ProductSalesOrg"]}',
                        ProductDistributionChnl = '{row["ProductDistributionChnl"]}',
                        SupplyingPlant = '{row["SupplyingPlant"]}',
                        AccountDetnProductGroup = '{row["AccountDetnProductGroup"]}',
                        ItemCategoryGroup = '{row["ItemCategoryGroup"]}',
                        MaterialGroup1 = '{row["FirstSalesSpecProductGroup"]}',
                        MaterialGroup2 = '{row["SecondSalesSpecProductGroup"]}',
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_ProductSalesDelivery (
                        Product, ProductSalesOrg, ProductDistributionChnl, SupplyingPlant, 
                        AccountDetnProductGroup, ItemCategoryGroup, MaterialGroup1, 
                        MaterialGroup2, UpdateDate
                    ) VALUES (
                        '{product}', '{row["ProductSalesOrg"]}', '{row["ProductDistributionChnl"]}', '{row["SupplyingPlant"]}', 
                        '{row["AccountDetnProductGroup"]}', '{row["ItemCategoryGroup"]}', '{row["FirstSalesSpecProductGroup"]}', 
                        '{row["SecondSalesSpecProductGroup"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. ยิงข้อมูลเข้า SQL Server ทุกๆ 100 แถว เพื่อความรวดเร็ว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกแถวที่เหลือ
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log เมื่อสำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductSalesDelivery','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductSalesDelivery : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductSalesDelivery','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task ProductSalesTax(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductSalesTax' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. ดึงข้อมูลทั้งหมดจาก OData มาพักไว้ใน Memory (DataTable)
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเพื่อป้องกัน SQL Error
                    string product = row["Product"].ToString().Replace("'", "''");
                    string country = row["Country"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL IF EXISTS เพื่อทำทั้ง Update และ Insert ในคำสั่งเดียว (Upsert)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_ProductSalesTax WHERE Product = '{product}')
                BEGIN
                    UPDATE Ms_ProductSalesTax SET 
                        Country = '{country}',
                        TaxCategory = '{row["TaxCategory"]}',
                        TaxClassification = '{row["TaxClassification"]}',
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_ProductSalesTax (Product, Country, TaxCategory, TaxClassification, UpdateDate)
                    VALUES ('{product}', '{country}', '{row["TaxCategory"]}', '{row["TaxClassification"]}', GETDATE());
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลไปประมวลผลที่ฐานข้อมูลเป็นชุด ชุดละ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกแถวที่เหลือ
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log สำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductSalesTax','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductSalesTax : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductSalesTax','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task ProductStorage(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductStorage' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. Fetch ข้อมูล OData ทั้งหมด
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    string product = row["Product"].ToString().Replace("'", "''");

                    // จัดการค่า Null หรือค่าว่างสำหรับตัวเลข เพื่อป้องกัน SQL Error
                    string minShelfLife = string.IsNullOrEmpty(row["MinRemainingShelfLife"]?.ToString()) ? "0" : row["MinRemainingShelfLife"].ToString();
                    string totalShelfLife = string.IsNullOrEmpty(row["TotalShelfLife"]?.ToString()) ? "0" : row["TotalShelfLife"].ToString();

                    // 3. ใช้ SQL Upsert Logic (IF EXISTS)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_ProductStorage WHERE Product = '{product}')
                BEGIN
                    UPDATE Ms_ProductStorage SET 
                        StorageCondition = '{row["StorageConditions"]}',
                        TemperatureCondition = '{row["TemperatureConditionInd"]}',
                        MinRemainingShelfLife = {minShelfLife},
                        TotalShelfLife = {totalShelfLife},
                        BaseUnit = '{row["BaseUnit"]}',
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_ProductStorage (
                        Product, StorageCondition, TemperatureCondition, 
                        MinRemainingShelfLife, TotalShelfLife, BaseUnit, UpdateDate
                    ) VALUES (
                        '{product}', '{row["StorageConditions"]}', '{row["TemperatureConditionInd"]}', 
                        {minShelfLife}, {totalShelfLife}, '{row["BaseUnit"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. Batch Execute ทุกๆ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductStorage','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductStorage : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductStorage','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task ProductUnitsOfMeasure(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่า
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'ProductUnitsOfMeasure' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. Fetch ข้อมูลทั้งหมดมาไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    string product = row["Product"].ToString().Replace("'", "''");
                    string altUnit = row["AlternativeUnit"].ToString().Replace("'", "''");

                    // ป้องกัน Error จากค่าตัวเลขที่เป็น Null หรือว่าง
                    string denominator = string.IsNullOrEmpty(row["QuantityDenominator"]?.ToString()) ? "0" : row["QuantityDenominator"].ToString();
                    string numerator = string.IsNullOrEmpty(row["QuantityNumerator"]?.ToString()) ? "0" : row["QuantityNumerator"].ToString();
                    string grossWeight = string.IsNullOrEmpty(row["GrossWeight"]?.ToString()) ? "0" : row["GrossWeight"].ToString();

                    // 3. ใช้ SQL Logic แบบ Upsert โดยเช็ค Composite Key (Product + AlternativeUnit)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_ProductUnitsOfMeasure WHERE Product = '{product}' AND AlternativeUnit = '{altUnit}')
                BEGIN
                    UPDATE Ms_ProductUnitsOfMeasure SET 
                        QuantityDenominator = {denominator},
                        QuantityNumerator = {numerator},
                        BaseUnit = '{row["BaseUnit"]}',
                        GrossWeight = {grossWeight},
                        WeightUnit = '{row["WeightUnit"]}',
                        UpdateDate = GETDATE()
                    WHERE Product = '{product}' AND AlternativeUnit = '{altUnit}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_ProductUnitsOfMeasure (
                        Product, AlternativeUnit, QuantityDenominator, QuantityNumerator, 
                        BaseUnit, GrossWeight, WeightUnit, UpdateDate
                    ) VALUES (
                        '{product}', '{altUnit}', {denominator}, {numerator}, 
                        '{row["BaseUnit"]}', {grossWeight}, '{row["WeightUnit"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลเข้า SQL เป็นชุด ชุดละ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log สำเร็จ (แก้ไขชื่อ Process ให้ตรงกับงาน)
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('ProductUnitsOfMeasure','Successful',getdate());", "dbDW");
                Console.WriteLine($"ProductUnitsOfMeasure : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('ProductUnitsOfMeasure','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task BusinessPartner(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinessPartner' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. ดึงข้อมูล OData ทั้งหมดมาพักไว้ใน DataTable (High-speed Fetch)
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลฟิลด์ที่เป็นข้อความเพื่อป้องกัน SQL Syntax Error
                    string bp = row["BusinessPartner"].ToString().Replace("'", "''");
                    string bpName = row["BusinessPartnerName"].ToString().Replace("'", "''").Replace(",", "");
                    string bpFull = row["BusinessPartnerFullName"].ToString().Replace("'", "''").Replace(",", "");
                    string org1 = row["OrganizationBPName1"].ToString().Replace("'", "''").Replace(",", "");
                    string org2 = row["OrganizationBPName2"].ToString().Replace("'", "''").Replace(",", "");
                    string firstName = row["FirstName"].ToString().Replace("'", "''").Replace(",", "");
                    string lastName = row["LastName"].ToString().Replace("'", "''").Replace(",", "");
                    string searchTerm1 = row["SearchTerm1"].ToString().Replace("'", "''");
                    string searchTerm2 = row["SearchTerm2"].ToString().Replace("'", "''");
                    // อย่าลืมใส่ไว้ด้านบนสุดของไฟล์


                    // 1. ระบุ Culture เป็นไทย เพื่อรองรับปี 2568
                    CultureInfo thaiCulture = new CultureInfo("th-TH");

                    // 2. ดึงค่าจาก row ออกมาเป็น string
                    string rawDate = row["CreationDate"].ToString();
                    string formattedCreation;
                    string formattedLastChange;
                    // 3. ใช้ TryParseExact เพื่อป้องกัน Error หาก String มี Format แปลกๆ
                    if (DateTime.TryParseExact(rawDate, "d/M/yyyy H:mm:ss", thaiCulture, DateTimeStyles.None, out DateTime tempDate))
                    {
                        // แปลงเป็น ค.ศ. และ Format ที่ต้องการ
                        formattedCreation = tempDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // กรณี Parse ไม่ผ่าน (เช่น ข้อมูลว่าง หรือ Format ไม่ตรงกับ d/M/yyyy H:mm:ss)
                        formattedCreation = "1900-01-01"; // หรือค่า Default อื่นๆ
                    }



                    string rawDateLastChange = row["LastChangeDate"].ToString();

                    // 3. ใช้ TryParseExact เพื่อป้องกัน Error หาก String มี Format แปลกๆ
                    if (DateTime.TryParseExact(rawDateLastChange, "d/M/yyyy H:mm:ss", thaiCulture, DateTimeStyles.None, out DateTime tempLastDate))
                    {
                        // แปลงเป็น ค.ศ. และ Format ที่ต้องการ
                        formattedLastChange = tempLastDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // กรณี Parse ไม่ผ่าน (เช่น ข้อมูลว่าง หรือ Format ไม่ตรงกับ d/M/yyyy H:mm:ss)
                        formattedLastChange = "1900-01-01"; // หรือค่า Default อื่นๆ
                    }

                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinessPartner WHERE BusinessPartner = '{bp}')
                BEGIN
                    UPDATE Ms_BusinessPartner SET 
                        Customer = '{row["Customer"]}', 
                        Supplier = '{row["Supplier"]}',
                        AcademicTitle = '{row["AcademicTitle"]}', 
                        BusinessPartnerCategory = '{row["BusinessPartnerCategory"]}',
                        BusinessPartnerGrouping = '{row["BusinessPartnerGrouping"]}', 
                        BusinessPartnerName = '{bpName}',
                        FirstName = '{firstName}', 
                        LastName = '{lastName}',
                        LastChangeDate = '{formattedLastChange}',
                        OrganizationBPName1 = '{org1}', 
                        OrganizationBPName2 = '{org2}',
                        SearchTerm1 = '{searchTerm1}', 
                        SearchTerm2 = '{searchTerm2}',
                        Etag = '{row["Etag"]}', 
                        CreationDate = '{formattedCreation}',
                        BusinessPartnerFull = '{bpFull}',
                        PersonNumber = '{row["PersonNumber"]}'
                    WHERE BusinessPartner = '{bp}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinessPartner (
                        [BusinessPartner], [Customer], [Supplier], [AcademicTitle], [AuthorizationGroup], 
                        [BusinessPartnerCategory], [BusinessPartnerFull], [BusinessPartnerGrouping], 
                        [BusinessPartnerName], [FirstName], [LastChangeDate], [OrganizationBPName1], 
                        [OrganizationBPName2], [SearchTerm1], [SearchTerm2], [Etag], [CreationDate], 
                        [LastName], [PersonNumber]
                    ) VALUES (
                        '{bp}', '{row["Customer"]}', '{row["Supplier"]}', '{row["AcademicTitle"]}', '{row["AuthorizationGroup"]}', 
                        '{row["BusinessPartnerCategory"]}', '{bpFull}', '{row["BusinessPartnerGrouping"]}', 
                        '{bpName}', '{firstName}', '{formattedLastChange}', '{org1}', 
                        '{org2}', '{searchTerm1}', '{searchTerm2}', '{row["Etag"]}', '{formattedCreation}', 
                        '{lastName}', '{row["PersonNumber"]}'
                    );
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลเข้า SQL Server เป็นชุด (Batch) ทุกๆ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // ส่งข้อมูลชุดสุดท้ายที่เหลือ
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log สำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinessPartner','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinessPartner : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinessPartner','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task BusinessPartnerAddress(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinessPartnerAddress' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาด String ฟิลด์ที่เสี่ยงต่อ SQL Error (ที่อยู่มักมีอักขระพิเศษ)
                    string bp = row["BusinessPartner"].ToString().Replace("'", "''");
                    string addrId = row["AddressID"].ToString().Replace("'", "''");
                    string fullName = row["FullName"].ToString().Replace("'", "''").Replace(",", "");
                    string houseNo = row["HouseNumber"].ToString().Replace("'", "''");
                    string streetPrefix = row["AdditionalStreetPrefixName"].ToString().Replace("'", "''").Replace(",", "");
                    string streetSuffix = row["AdditionalStreetSuffixName"].ToString().Replace("'", "''").Replace(",", "");
                    string cityName = row["CityName"].ToString().Replace("'", "''");
                    string careOf = row["CareOfName"].ToString().Replace("'", "''");

                    // ใช้ SQL Logic: IF EXISTS โดยใช้ Composite Key (BusinessPartner + AddressID)

                    // 1. ระบุ Culture เป็นไทย เพื่อรองรับปี 2568
                    CultureInfo thaiCulture = new CultureInfo("th-TH");


                    // 2. ดึงค่าจาก row ออกมาเป็น string
                    string rawDateEnd = row["ValidityEndDate"].ToString();
                    string rawDateStart = row["ValidityStartDate"].ToString();
                    string formattedValidityEndDate;
                    string formattedValidityStartDate;

                    // 3. ใช้ TryParseExact เพื่อป้องกัน Error หาก String มี Format แปลกๆ
                    if (DateTime.TryParseExact(rawDateStart, "d/M/yyyy H:mm:ss", thaiCulture, DateTimeStyles.None, out DateTime tempDate))
                    {
                        // แปลงเป็น ค.ศ. และ Format ที่ต้องการ
                        formattedValidityStartDate = tempDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // กรณี Parse ไม่ผ่าน (เช่น ข้อมูลว่าง หรือ Format ไม่ตรงกับ d/M/yyyy H:mm:ss)
                        formattedValidityStartDate = "1900-01-01"; // หรือค่า Default อื่นๆ
                    }



                    // 3. ใช้ TryParseExact เพื่อป้องกัน Error หาก String มี Format แปลกๆ
                    if (DateTime.TryParseExact(rawDateEnd, "d/M/yyyy H:mm:ss", thaiCulture, DateTimeStyles.None, out DateTime tempDateEnd))
                    {
                        // แปลงเป็น ค.ศ. และ Format ที่ต้องการ
                        formattedValidityEndDate = tempDateEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // กรณี Parse ไม่ผ่าน (เช่น ข้อมูลว่าง หรือ Format ไม่ตรงกับ d/M/yyyy H:mm:ss)
                        formattedValidityEndDate = "1900-01-01"; // หรือค่า Default อื่นๆ
                    }


                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinessPartnerAddress WHERE BusinessPartner = '{bp}' AND AddressID = '{addrId}')
                BEGIN
                    UPDATE Ms_BusinessPartnerAddress SET 
                        ValidityStartDate = '{formattedValidityStartDate}', ValidityEndDate = '{formattedValidityEndDate}',
                        AuthorizationGroup = '{row["AuthorizationGroup"]}', AddressUUID = '{row["AddressUUID"]}',
                        AdditionalStreetPrefixName = '{streetPrefix}', AdditionalStreetSuffixName = '{streetSuffix}',
                        FullName = '{fullName}', HomeCityName = '{row["HomeCityName"]}',
                        HouseNumber = '{houseNo}', HouseNumberSupplementText = '{row["HouseNumberSupplementText"]}',
                        CareOfName = '{careOf}', CityName = '{cityName}',
                        District = '{row["District"]}', PostCode = '{row["PostalCode"]}',
                        Country = '{row["Country"]}', Person = '{row["Person"]}',
                        StreetPrefixName = '{row["StreetPrefixName"].ToString().Replace("'", "''")}', UpdateDate = GETDATE()
                    WHERE BusinessPartner = '{bp}' AND AddressID = '{addrId}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinessPartnerAddress (
                        [BusinessPartner],[AddressID],[ValidityStartDate],[ValidityEndDate],[AuthorizationGroup],[AddressUUID],
                        [AdditionalStreetPrefixName],[AdditionalStreetSuffixName],[FullName],[HomeCityName],[HouseNumber],
                        [HouseNumberSupplementText],[CareOfName],[CityName],[District],[PostCode],[Country],[Person],
                        [StreetPrefixName],[StreetSuffixName],UpdateDate
                    ) VALUES (
                        '{bp}', '{addrId}', '{formattedValidityStartDate}', '{formattedValidityEndDate}', '{row["AuthorizationGroup"]}',
                        '{row["AddressUUID"]}', '{streetPrefix}', '{streetSuffix}', '{fullName}', '{row["HomeCityName"]}',
                        '{houseNo}', '{row["HouseNumberSupplementText"]}', '{careOf}', '{cityName}', '{row["District"]}',
                        '{row["PostalCode"]}', '{row["Country"]}', '{row["Person"]}', '{row["StreetPrefixName"].ToString().Replace("'", "''")}', 
                        '{row["StreetSuffixName"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // ส่งข้อมูลเข้า DB ชุดละ 100 แถว เพื่อลด Network Latency
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinessPartnerAddress','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinessPartnerAddress : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinessPartnerAddress','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task BusinessPartnerEmail(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinessPartnerEmail' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    string addrId = row["AddressID"].ToString().Replace("'", "''");
                    string ordinal = row["OrdinalNumber"].ToString().Replace("'", "''");
                    string person = row["Person"].ToString().Replace("'", "''");
                    string email = row["EmailAddress"].ToString().Replace("'", "''").Replace(",", "");

                    // ใช้ Composite Key ในการเช็ค (AddressID + OrdinalNumber + Person)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinessPartnerEmail WHERE OrdinalNumber = '{ordinal}' AND AddressID = '{addrId}' AND Person = '{person}')
                BEGIN
                    UPDATE Ms_BusinessPartnerEmail SET 
                        Email = '{email}', 
                        UpdateDate = GETDATE()
                    WHERE OrdinalNumber = '{ordinal}' AND AddressID = '{addrId}' AND Person = '{person}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinessPartnerEmail (AddressID, OrdinalNumber, Email, Person, UpdateDate)
                    VALUES ('{addrId}', '{ordinal}', '{email}', '{person}', GETDATE());
                END");

                    rowCount++;

                    // ส่งข้อมูลเข้า DB เป็น Batch ชุดละ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinessPartnerEmail','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinessPartnerEmail : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinessPartnerEmail','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task BusinessPartnerPhone(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinessPartnerPhone' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเบื้องต้น
                    string addrId = row["AddressID"].ToString().Replace("'", "''");
                    string ordinal = row["OrdinalNumber"].ToString().Replace("'", "''");
                    string person = row["Person"].ToString().Replace("'", "''");
                    string phone = row["PhoneNumber"].ToString().Replace("'", "''").Replace(",", "");
                    string intPhone = row["InternationalPhoneNumber"].ToString().Replace("'", "''").Replace(",", "");
                    string remark = row["AddressCommunicationRemarkText"].ToString().Replace("'", "''").Replace(",", "");

                    // ใช้ SQL "UPSERT" Logic เช็ค Composite Key (AddressID + OrdinalNumber + Person)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinessPartnerPhone WHERE OrdinalNumber = '{ordinal}' AND AddressID = '{addrId}' AND Person = '{person}')
                BEGIN
                    UPDATE Ms_BusinessPartnerPhone SET 
                        PhoneNumber = '{phone}',
                        InternationalPhoneNumber = '{intPhone}',
                        AddressCommunicationRemarkText = '{remark}',
                        UpdateDate = GETDATE()
                    WHERE OrdinalNumber = '{ordinal}' AND AddressID = '{addrId}' AND Person = '{person}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinessPartnerPhone (
                        AddressID, OrdinalNumber, PhoneNumber, InternationalPhoneNumber, 
                        Person, AddressCommunicationRemarkText, UpdateDate
                    ) VALUES (
                        '{addrId}', '{ordinal}', '{phone}', '{intPhone}', 
                        '{person}', '{remark}', GETDATE()
                    );
                END");

                    rowCount++;

                    // ส่งข้อมูลเข้า DB ชุดละ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinessPartnerPhone','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinessPartnerPhone : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinessPartnerPhone','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task BusinnessPartnerCustomer(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinnessPartnerCustomer' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. ดึงข้อมูล OData ทั้งหมดมาพักไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลฟิลด์ String ป้องกัน SQL Error
                    string customer = row["Customer"].ToString().Replace("'", "''");
                    string customerFullName = row["CustomerFullName"].ToString().Replace("'", "''").Replace(",", "");
                    string bpCustomerFullName = row["BPCustomerFullName"].ToString().Replace("'", "''").Replace(",", "");
                    string customerName = row["CustomerName"].ToString().Replace("'", "''").Replace(",", "");
                    string taxNumber3 = row["TaxNumber3"].ToString().Replace("'", "''").Replace(",", "");

                    // 3. ใช้ SQL IF EXISTS เพื่อจัดการ Upsert ในระดับ Database
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinnessPartnerCustomer WHERE Customer = '{customer}')
                BEGIN
                    UPDATE Ms_BusinnessPartnerCustomer SET 
                        AuthorizationGroup = '{row["AuthorizationGroup"]}',
                        CustomerAccountGroup = '{row["CustomerAccountGroup"]}',
                        CustomerFullName = '{customerFullName}',
                        BPCustomerFullName = '{bpCustomerFullName}',
                        CustomerName = '{customerName}',
                        DeliveryIsBlocked = '{row["DeliveryIsBlocked"]}',
                        TaxNumber3 = '{taxNumber3}',
                        UpdateDate = GETDATE()
                    WHERE Customer = '{customer}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinnessPartnerCustomer (
                        [Customer], [AuthorizationGroup], [CustomerAccountGroup], 
                        [CustomerFullName], [BPCustomerFullName], [CustomerName], 
                        [DeliveryIsBlocked], [TaxNumber3], UpdateDate
                    ) VALUES (
                        '{customer}', '{row["AuthorizationGroup"]}', '{row["CustomerAccountGroup"]}', 
                        '{customerFullName}', '{bpCustomerFullName}', '{customerName}', 
                        '{row["DeliveryIsBlocked"]}', '{taxNumber3}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. Batch Execute ทุกๆ 100 แถว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log เมื่อสำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinnessPartnerCustomer','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinnessPartnerCustomer : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinnessPartnerCustomer','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task BusinnessPartnerCustomerCompany(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การดึงข้อมูลครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinnessPartnerCustomerCompany' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. Fetch ข้อมูลจาก SAP OData ทั้งหมด
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเบื้องต้น
                    string customer = row["Customer"].ToString().Replace("'", "''");
                    string company = row["CompanyCode"].ToString().Replace("'", "''");
                    string payTerm = row["PaymentTerms"].ToString().Replace("'", "''").Replace(",", "");
                    string reconAcc = row["ReconciliationAccount"].ToString().Replace("'", "''").Replace(",", "");

                    // 3. ใช้ SQL Logic เช็ค Composite Key (Customer + CompanyCode) 
                    // เพื่อจัดการ Upsert ในระดับ Database Command เดียว
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinnessPartnerCustomerCompany WHERE Customer = '{customer}' AND CompanyCode = '{company}')
                BEGIN
                    UPDATE Ms_BusinnessPartnerCustomerCompany SET 
                        PaymentTerms = '{payTerm}',
                        ReconciliationAccount = '{reconAcc}',
                        UpdateDate = GETDATE()
                    WHERE Customer = '{customer}' AND CompanyCode = '{company}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinnessPartnerCustomerCompany (Customer, CompanyCode, PaymentTerms, ReconciliationAccount, UpdateDate)
                    VALUES ('{customer}', '{company}', '{payTerm}', '{reconAcc}', GETDATE());
                END");

                    rowCount++;

                    // 4. Batch Execute ทุกๆ 100 แถว เพื่อลด Network Latency
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกแถวที่เหลือ
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log สำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinnessPartnerCustomerCompany','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinnessPartnerCustomerCompany : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinnessPartnerCustomerCompany','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task BusinnessPartnerCustomerSalesaArea(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinnessPartnerCustomerSalesaArea' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. Fetch ข้อมูล OData ทั้งหมดมาพักไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลฟิลด์ที่ใช้เป็น Key และฟิลด์ข้อความ
                    string customer = row["Customer"].ToString().Replace("'", "''");
                    string distChannel = row["DistributionChannel"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL "UPSERT" (IF EXISTS) เพื่อลดภาระ Network Round-trip
                    // โดยเช็ค Composite Key: Customer + DistributionChannel
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinnessPartnerCustomerSalesaArea WHERE Customer = '{customer}' AND DistributionChannel = '{distChannel}')
                BEGIN
                    UPDATE Ms_BusinnessPartnerCustomerSalesaArea SET 
                        SalesOrganization = '{row["SalesOrganization"]}',
                        Division = '{row["Division"]}',
                        Currency = '{row["Currency"]}',
                        CustomerAccountAssignmentGroup = '{row["CustomerAccountAssignmentGroup"]}',
                        CustomerGroup = '{row["CustomerGroup"]}',
                        CustomerPaymentTerms = '{row["CustomerPaymentTerms"]}',
                        CustomerPricingProcedure = '{row["CustomerPricingProcedure"]}',
                        SalesOffice = '{row["SalesOffice"]}',
                        ShippingCondition = '{row["ShippingCondition"]}',
                        SalesDistrict = '{row["SalesDistrict"]}',
                        ExchangeRateType = '{row["ExchangeRateType"]}',
                        AdditionalCustomerGroup1 = '{row["AdditionalCustomerGroup1"]}',
                        AdditionalCustomerGroup2 = '{row["AdditionalCustomerGroup2"]}',
                        AdditionalCustomerGroup3 = '{row["AdditionalCustomerGroup3"]}',
                        AdditionalCustomerGroup4 = '{row["AdditionalCustomerGroup4"]}',
                        CustomerAccountGroup = '{row["CustomerAccountGroup"]}',
                        UpdateDate = GETDATE()
                    WHERE Customer = '{customer}' AND DistributionChannel = '{distChannel}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinnessPartnerCustomerSalesaArea (
                        [Customer],[SalesOrganization],[DistributionChannel],[Division],[Currency],
                        [CustomerAccountAssignmentGroup],[CustomerGroup],[CustomerPaymentTerms],
                        [CustomerPricingProcedure],[SalesOffice],[ShippingCondition],[SalesDistrict],
                        ExchangeRateType,[AdditionalCustomerGroup1],[AdditionalCustomerGroup2],
                        [AdditionalCustomerGroup3],[AdditionalCustomerGroup4],[CustomerAccountGroup],UpdateDate
                    ) VALUES (
                        '{customer}', '{row["SalesOrganization"]}', '{distChannel}', '{row["Division"]}', 
                        '{row["Currency"]}', '{row["CustomerAccountAssignmentGroup"]}', '{row["CustomerGroup"]}', 
                        '{row["CustomerPaymentTerms"]}', '{row["CustomerPricingProcedure"]}', '{row["SalesOffice"]}', 
                        '{row["ShippingCondition"]}', '{row["SalesDistrict"]}', '{row["ExchangeRateType"]}', 
                        '{row["AdditionalCustomerGroup1"]}', '{row["AdditionalCustomerGroup2"]}', 
                        '{row["AdditionalCustomerGroup3"]}', '{row["AdditionalCustomerGroup4"]}', 
                        '{row["CustomerAccountGroup"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. Batch Execute ทุกๆ 100 แถว เพื่อความเร็วสูงสุด
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log สำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinnessPartnerCustomerSalesaArea','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinnessPartnerCustomerSalesaArea : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinnessPartnerCustomerSalesaArea','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }



        private static async Task BusinessPartnerCustSalesPartnerFunc(string sText)
        {
            var batchSql = new StringBuilder();
            int rowCount = 0;
            string _url = "";

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียวจากฐานข้อมูล
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'BusinessPartnerCustSalesPartnerFunc' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 2. Fetch ข้อมูลทั้งหมดจาก OData มาพักไว้ใน Memory (DataTable)
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                foreach (DataRow row in dt.Rows)
                {
                    // ทำความสะอาดข้อมูลเพื่อป้องกัน SQL Error
                    string customer = row["Customer"].ToString().Replace("'", "''");
                    string partnerFunc = row["PartnerFunction"].ToString().Replace("'", "''");
                    string salesOrg = row["SalesOrganization"].ToString().Replace("'", "''");
                    string distChannel = row["DistributionChannel"].ToString().Replace("'", "''");
                    string division = row["Division"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL "UPSERT" Logic (IF EXISTS) โดยเช็ค Composite Key (Customer + PartnerFunction)
                    // หมายเหตุ: แก้ไข Logic การ Insert ให้ใส่ DistributionChannel ให้ถูกต้อง (เดิมใน Code คุณใส่ SalesOrg ซ้ำ)
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Ms_BusinessPartnerCustSalesPartnerFunc WHERE Customer = '{customer}' AND PartnerFunction = '{partnerFunc}' and SalesOrganization = '{salesOrg}')
                BEGIN
                    UPDATE Ms_BusinessPartnerCustSalesPartnerFunc SET 
                        SalesOrganization = '{salesOrg}',
                        DistributionChannel = '{distChannel}',
                        Division = '{division}',
                        BPCustomerNumber = '{row["BPCustomerNumber"]}',
                        AuthorizationGroup = '{row["AuthorizationGroup"]}',
                        UpdateDate = GETDATE()
                    WHERE Customer = '{customer}' AND PartnerFunction = '{partnerFunc}' and SalesOrganization = '{salesOrg}';
                END
                ELSE
                BEGIN
                    INSERT INTO Ms_BusinessPartnerCustSalesPartnerFunc (
                        [Customer], [SalesOrganization], [DistributionChannel], [Division], 
                        [PartnerFunction], [BPCustomerNumber], [AuthorizationGroup], [CreateDate]
                    ) VALUES (
                        '{customer}', '{salesOrg}', '{distChannel}', '{division}', 
                        '{partnerFunc}', '{row["BPCustomerNumber"]}', '{row["AuthorizationGroup"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลเข้า DB เป็นก้อน (Batch) ชุดละ 100 แถว เพื่อความรวดเร็ว
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกชุดสุดท้ายที่เหลือ
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึกสถานะการทำงานสำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('BusinessPartnerCustSalesPartnerFunc','Successful',getdate());", "dbDW");
                Console.WriteLine($"BusinessPartnerCustSalesPartnerFunc : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BusinessPartnerCustSalesPartnerFunc','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }



        private static async Task AddSalesOrder(string sText)
        {
            var batchSql = new StringBuilder();
            string _url = "";
            string _DocNo = "";
            DateTime Getdate = DateTime.Today;
            DateTime LastDate = DateTime.Today.AddDays(-3);

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'SalesOrder' and Isactive = 1 and DataSource = 'OData'", "dbDW");
                _url += $"?$filter=CreationDate ge datetime'{LastDate:yyyy-MM-dd}T00:00:00' and CreationDate le datetime'{Getdate:yyyy-MM-dd}T23:59:59'";

                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);

                if (dt.Rows.Count == 0) return;

                // รายการ Task สำหรับรันงานย่อยแบบขนาน
                var subTasks = new List<Task>();

                foreach (DataRow row in dt.Rows)
                {
                    string soNumber = row["SalesOrder"].ToString().Replace("'", "''");

                    // 1. ใช้ SQL Logic (IF NOT EXISTS) เพื่อลดการยิง GetDataTable เช็คทีละแถว
                    batchSql.AppendLine($@"
                IF NOT EXISTS (SELECT 1 FROM Op_SalesOrder WHERE SalesOrder = '{soNumber}')
                BEGIN
                    INSERT INTO Op_SalesOrder (
                        [SalesOrder],[System_Name],[SalesOrderDate],[SalesOrderType],[SalesOrderTypeInternalCode],[SalesOrganization],
                        [DistributionChannel],[SalesGroup],[SalesOffice],[SalesDistrict],[SoldToParty],[PurchaseOrderByCustomer],
                        [TotolNetAmont],[OverallDeliveryStatus],[TrabsactionCurrency],[PricingDate],[PriceDetnExchangeRate],
                        [RequestedDeliveryDate],[CustomerPaymentTerms],[OverallISDProcessStatus],[OverallTotalDeliveryStatus],[BillingDoucumentDate]
                    ) VALUES (
                        '{soNumber}', 'S4HC', '{row["SalesOrderDate"]}', '{row["SalesOrderType"]}', '{row["SalesOrderTypeInternalCode"]}', '{row["SalesOrganization"]}', 
                        '{row["DistributionChannel"]}', '{row["SalesGroup"]}', '{row["SalesOffice"]}', '{row["SalesDistrict"]}', '{row["SoldToParty"]}', '{row["PurchaseOrderByCustomer"]}', 
                        '{row["TotalNetAmount"]}', '{row["OverallDeliveryStatus"]}', '{row["TransactionCurrency"]}', '{row["PricingDate"]}', '{row["PriceDetnExchangeRate"]}', 
                        '{row["RequestedDeliveryDate"]}', '{row["CustomerPaymentTerms"]}', '{row["OverallSDProcessStatus"]}', '{row["OverallTotalDeliveryStatus"]}', '{row["BillingDocumentDate"]}'
                    );
                END");

                    // 2. เพิ่ม Task งานย่อยเข้าไปในคิวเพื่อรันพร้อมกัน
                    subTasks.Add(SalesOrderHeaderPartner(soNumber));
                    subTasks.Add(SalesOrderItem(soNumber, row["SoldToParty"].ToString(), row["SalesOrganization"].ToString()));

                    // 3. เพื่อไม่ให้ SQL Script ยาวเกินไป เราจะส่งไปประมวลผลทุกๆ 50 SO
                    if (subTasks.Count >= 100) // 50 SO * 2 tasks per SO
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                        await Task.WhenAll(subTasks); // รอนานงานย่อยชุดนี้ให้จบ
                        subTasks.Clear();
                    }
                }

                // เก็บตกชุดสุดท้าย
                if (batchSql.Length > 0) SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                if (subTasks.Count > 0) await Task.WhenAll(subTasks);

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('Add Sales Order','Successful',getdate());", "dbDW");
                Console.WriteLine("AddSalesOrder : Sync Successful");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('Add Sales Order','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task UpdateSalesOrder(string sText)
        {
            var batchSql = new StringBuilder();
            string _url = "";
            DateTime Getdate = DateTime.Today;
            DateTime LastDate = DateTime.Today.AddDays(-3);

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'SalesOrder' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // ใช้ LastChangeDateTime เพื่อดึงเฉพาะรายการที่มีการอัปเดต
                _url += $"?$filter=LastChangeDateTime ge datetimeoffset'{LastDate:yyyy-MM-dd}T00:00:00Z' and LastChangeDateTime le datetimeoffset'{Getdate:yyyy-MM-dd}T23:59:59Z'";

                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);
                if (dt.Rows.Count == 0) return;

                var subTasks = new List<Task>();

                foreach (DataRow row in dt.Rows)
                {
                    string soNumber = row["SalesOrder"].ToString().Replace("'", "''");

                    // 1. ใช้ StringBuilder ต่อ Query สำหรับ Batch Update
                    // ใช้ IF EXISTS เพื่อให้ SQL Server ตรวจสอบเอง ลดการ Query จากฝั่ง C#
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Op_SalesOrder WHERE SalesOrder = '{soNumber}')
                BEGIN
                    UPDATE Op_SalesOrder SET 
                        SalesOrderDate = '{row["SalesOrderDate"]}', SalesOrderType = '{row["SalesOrderType"]}',
                        SalesOrderTypeInternalCode = '{row["SalesOrderTypeInternalCode"]}', SalesOrganization = '{row["SalesOrganization"]}',
                        DistributionChannel = '{row["DistributionChannel"]}', SalesGroup = '{row["SalesGroup"]}',
                        SalesOffice = '{row["SalesOffice"]}', SalesDistrict = '{row["SalesDistrict"]}',
                        SoldToParty = '{row["SoldToParty"]}', PurchaseOrderByCustomer = '{row["PurchaseOrderByCustomer"].ToString().Replace("'", "''")}',
                        TotolNetAmont = '{row["TotalNetAmount"]}', OverallDeliveryStatus = '{row["OverallDeliveryStatus"]}',
                        TrabsactionCurrency = '{row["TransactionCurrency"]}', PricingDate = '{row["PricingDate"]}',
                        PriceDetnExchangeRate = '{row["PriceDetnExchangeRate"]}', RequestedDeliveryDate = '{row["RequestedDeliveryDate"]}',
                        CustomerPaymentTerms = '{row["CustomerPaymentTerms"]}', OverallISDProcessStatus = '{row["OverallSDProcessStatus"]}',
                        OverallTotalDeliveryStatus = '{row["OverallTotalDeliveryStatus"]}', BillingDoucumentDate = '{row["BillingDocumentDate"]}'
                    WHERE SalesOrder = '{soNumber}';
                END");

                    // 2. เตรียม Task สำหรับอัปเดตข้อมูลประกอบ (Items / Partners)
                    subTasks.Add(SalesOrderHeaderPartner(soNumber));
                    subTasks.Add(SalesOrderItem(soNumber, row["SoldToParty"].ToString(), row["SalesOrganization"].ToString()));

                    // 3. ควบคุม Batch Size เพื่อประสิทธิภาพ
                    if (subTasks.Count >= 50)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                        await Task.WhenAll(subTasks); // รันงานประกอบ SO ชุดนี้พร้อมกัน
                        subTasks.Clear();
                    }
                }

                // เก็บตกรายการที่เหลือ
                if (batchSql.Length > 0) SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                if (subTasks.Count > 0) await Task.WhenAll(subTasks);

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('Update Sales Order','Successful',getdate());", "dbDW");
                Console.WriteLine("UpdateSalesOrder : Sync Successful");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('Update Sales Order','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }



        private static async Task SalesOrderItem(string SONumber, string CustomerCode, string SalesOrganization)
        {
            var batchSql = new StringBuilder();
            string _url = "";

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'SalesOrderItem' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                string filter = $"SalesOrder eq '{SONumber}'";
                string url = $"{_url}?$filter={Uri.EscapeDataString(filter)}&$format=json";

                // 1. ดึงข้อมูลจาก SAP OData
                DataTable dt = await helper.FetchODataSinglePageAsync(url, _USERNAME, _PASSWORD);
                if (dt.Rows.Count == 0) return;

                // 2. ลบข้อมูลเก่าของ SO นี้ออกก่อนเริ่ม Insert ใหม่ (ลบทีเดียวข้างนอก)
                SQLConnect.Updatedata($"DELETE Op_SalesOrderItem WHERE SalesOrder = '{SONumber}'", "dbDW");

                // 3. เตรียม Master Data พักไว้ใน Dictionary (In-Memory) เพื่อลดการยิง SQL ใน Loop
                var matGroupDict = new Dictionary<string, (string Code, string Name)>();
                string areaCode = "", areaName = "";

                if (SalesOrganization == "2000")
                {
                    // ดึง Area ของ Customer นี้มาทีเดียว
                    DataTable dtArea = SQLConnect.GetDataTable($@"
                SELECT SA.AdditionalCustomerGroup3, CG.Description 
                FROM Ms_BusinnessPartnerCustomerSalesaArea SA
                LEFT JOIN Ms_CustomerGroup3 CG ON SA.AdditionalCustomerGroup3 = CG.CustomerGroup
                WHERE SA.Customer = '{CustomerCode}'", "dbDW");

                    if (dtArea.Rows.Count > 0)
                    {
                        areaCode = dtArea.Rows[0]["AdditionalCustomerGroup3"].ToString();
                        areaName = dtArea.Rows[0]["Description"].ToString();
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    string material = row["Material"].ToString();
                    string saleCode = "", saleName = "", matGroup2Code = "", matGroup2Name = "";

                    if (SalesOrganization == "2000")
                    {
                        // Lookup Material Group 2 จากตาราง Product (ใช้ Cache ใน Memory เพื่อลดภาระ DB)
                        if (!matGroupDict.ContainsKey(material))
                        {
                            DataTable dtMat = SQLConnect.GetDataTable($@"
                        SELECT SD.MaterialGroup2, MG.Description 
                        FROM Ms_ProductSalesDelivery SD
                        LEFT JOIN Ms_MaterialGroup2 MG ON SD.MaterialGroup2 = MG.MaterialGroupCode
                        WHERE SD.Product = '{material}'", "dbDW");

                            if (dtMat.Rows.Count > 0)
                                matGroupDict[material] = (dtMat.Rows[0][0].ToString(), dtMat.Rows[0][1].ToString());
                            else
                                matGroupDict[material] = ("", "");
                        }

                        var matInfo = matGroupDict[material];
                        matGroup2Code = matInfo.Code;
                        matGroup2Name = matInfo.Name;

                        // Lookup Sales Employee จาก Area และ Division (Material Group 2)
                        DataTable dtSales = SQLConnect.GetDataTable($@"
                   SELECT  [BusinessPartner],[BusinessPartnerName] FROM  [Ms_BusinessPartner] 
                    WHERE BusinessPartner = '{row["YY1_SDSalesEmployeeI_SDI"]}'", "dbDW");

                        if (dtSales.Rows.Count > 0)
                        {
                            saleCode = dtSales.Rows[0]["BusinessPartner"].ToString();
                            saleName = dtSales.Rows[0]["BusinessPartnerName"].ToString();
                        }
                    }
                    else if (SalesOrganization == "1000")
                    {
                        DataTable dtMap = SQLConnect.GetDataTable($"SELECT Customer, FullName FROM Mapping_SalesEmployee_transaction WHERE SalesOrder = '{SONumber}'", "dbDW");
                        if (dtMap.Rows.Count > 0)
                        {
                            saleCode = dtMap.Rows[0]["Customer"].ToString();
                            saleName = dtMap.Rows[0]["FullName"].ToString();
                        }
                    }

                    // 4. สร้าง Batch Insert Script
                    batchSql.AppendLine($@"
                INSERT INTO Op_SalesOrderItem (
                    [SalesOrder],[SalesOrderItem],[SalesOrderItemCategory],[SalesOrderItemText],[Material],
                    [RequestedQuantity],[RequestedQuantityUnit],[NetAmount],[TaxAmount],[CostAmount],
                    SalesCode, SalesName, AreaCode, AreaName, MatGroup2, MatGroupName2
                ) VALUES (
                    '{row["SalesOrder"]}', '{row["SalesOrderItem"]}', '{row["SalesOrderItemCategory"]}', 
                    '{row["SalesOrderItemText"].ToString().Replace("'", "''")}', '{material}',
                    '{row["RequestedQuantity"]}', '{row["RequestedQuantityUnit"]}', '{row["NetAmount"]}', 
                    '{row["TaxAmount"]}', '{row["CostAmount"]}',
                    '{saleCode}', '{saleName.Replace("'", "''")}', '{areaCode}', '{areaName.Replace("'", "''")}', 
                    '{matGroup2Code}', '{matGroup2Name.Replace("'", "''")}'
                );");
                }

                // 5. ยิง SQL ทีเดียวจบงาน
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                Console.WriteLine($"SalesOrderItem {SONumber} : Sync Successful");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('SalesOrderItem','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task SalesOrderHeaderPartner(string SONumber)
        {
            var batchSql = new StringBuilder();
            string _url = "";
            string _addressID = "";
            string _CheckAddress = "";

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'SalesOrderHeaderPartner' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                string filter = $"SalesOrder eq '{SONumber}'";
                string url = $"{_url}?$filter={Uri.EscapeDataString(filter)}&$format=json";


                // 1. ดึงข้อมูลจาก SAP OData
                DataTable dt = await helper.FetchODataSinglePageAsync(url, _USERNAME, _PASSWORD);
                if (dt.Rows.Count == 0) return;

                // 2. ลบข้อมูลเดิมของ SO นี้ออกก่อนเริ่ม Sync ใหม่ (ทำครั้งเดียวเพื่อความเร็ว)
                SQLConnect.Updatedata($"DELETE Op_SalesOrderHeaderPartner WHERE SalesOrder = '{SONumber}'", "dbDW");

                // 3. ใช้ StringBuilder รวมคำสั่ง Insert ของทุก Partner
                foreach (DataRow row in dt.Rows)
                {
                    _addressID = SQLConnect.GetStringValue("SELECT  [AddressID] FROM [MGT_Datawarehouse].[dbo].[Ms_BusinessPartnerAddress] where [AddressID] = '" + row["AddressID"] + "' ", "dbDW");

                    if (_addressID == "")

                    {
                        int number = int.Parse(row["AddressID"].ToString());

                        _addressID = Convert.ToString(number - 1);

                        _CheckAddress = SQLConnect.GetStringValue("SELECT  [AddressID] FROM [MGT_Datawarehouse].[dbo].[Ms_BusinessPartnerAddress] where [AddressID] = '" + _addressID + "'", "dbDW");

                        if (_CheckAddress == "")
                        {
                            _CheckAddress = "";

                        }

                    }
                    else
                    {
                        _CheckAddress = row["AddressID"].ToString();
                    }

                    batchSql.AppendLine($@"
                INSERT INTO Op_SalesOrderHeaderPartner (
                    [SalesOrder], [PartnerFunction], [PartnerFunctionInternalCode], [Customer], [AddressID]
                ) VALUES (
                    '{row["SalesOrder"]}', '{row["PartnerFunction"]}', 
                    '{row["PartnerFunctionInternalCode"]}', '{row["Customer"]}', '{_CheckAddress}'
                );");
                }

                // 4. ส่งคำสั่ง Insert ทั้งหมดไปประมวลผลทีเดียว (Batch Execute)
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                Console.WriteLine($"SalesOrderHeaderPartner {SONumber} : Sync Successful");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('SalesOrderHeaderPartner','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }



        private static async Task RecheckcSalesOrderHeaderPartner(string SONumber)
        {
            var batchSql = new StringBuilder();
            string _url = "";

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue("SELECT [DataSyntax] FROM [Setting_SyncData] where DataType = 'SalesOrderHeaderPartner' and Isactive = 1 and DataSource = 'OData'", "dbDW");

                // 1. ดึงรายการ SO ที่ตกหล่น (Sales Employee is null) ขึ้นมาทั้งหมด
                DataTable dtRecheck = SQLConnect.GetDataTable("SELECT [SalesDocument] FROM [MGT_Datawarehouse].[dbo].[MGT_Sale] where [SalesEmployeeTX] is null", "dbDW");

                if (dtRecheck.Rows.Count == 0) return;

                // 2. สร้าง List ของ Task เพื่อดึงข้อมูล OData แบบขนาน (Parallel)
                var fetchTasks = new List<Task<(string SODoc, DataTable Data)>>();

                foreach (DataRow row in dtRecheck.Rows)
                {
                    string soDoc = row["SalesDocument"].ToString();
                    string filter = $"SalesOrder eq '{soDoc}'";
                    string url = $"{_url}?$filter={Uri.EscapeDataString(filter)}&$format=json";

                    // เพิ่ม Task เข้า List (ยังไม่รันแบบรอผลทีละตัว)
                    fetchTasks.Add(Task.Run(async () => {
                        var dt = await helper.FetchODataSinglePageAsync(url, _USERNAME, _PASSWORD);
                        return (soDoc, dt);
                    }));
                }

                // 3. รัน Task ทั้งหมดพร้อมกันเพื่อประหยัดเวลา OData Fetch
                var results = await Task.WhenAll(fetchTasks);

                foreach (var result in results)
                {
                    if (result.Data.Rows.Count > 0)
                    {
                        // ลบข้อมูลเดิมของ SO ที่ตกหล่นรายนี้ออกก่อน
                        batchSql.AppendLine($"DELETE Op_SalesOrderHeaderPartner WHERE SalesOrder = '{result.SODoc}';");

                        // รวมคำสั่ง Insert ของทุก Partner ในคราวเดียว
                        foreach (DataRow rowIns in result.Data.Rows)
                        {
                            batchSql.AppendLine($@"
                        INSERT INTO Op_SalesOrderHeaderPartner (
                            [SalesOrder], [PartnerFunction], [PartnerFunctionInternalCode], [Customer], [AddressID]
                        ) VALUES (
                            '{rowIns["SalesOrder"]}', '{rowIns["PartnerFunction"]}', 
                            '{rowIns["PartnerFunctionInternalCode"]}', '{rowIns["Customer"]}', '{rowIns["AddressID"]}'
                        );");
                        }

                        // 4. ส่ง Batch SQL เข้า DB ทุกๆ 20 SO เพื่อไม่ให้ String ยาวเกินไป
                        if (batchSql.Length > 5000)
                        {
                            SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                            batchSql.Clear();
                        }
                    }
                }

                // เก็บตก Batch สุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                Console.WriteLine($"Recheck SalesOrderHeaderPartner : Completed ({results.Length} documents processed)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('RecheckPartner','Fail',getdate(),'{errMsg}');", "dbDW");
                Console.WriteLine(errMsg);
            }
        }


        private static async Task AddBilling(string sText)
        {
            var batchSql = new StringBuilder();
            string _url = "";
            DateTime Getdate = DateTime.Today;
            DateTime LastDate = DateTime.Today.AddDays(-3);

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียว
                _url = SQLConnect.GetStringValue(
                    "SELECT [DataSyntax] FROM [Setting_SyncData] WHERE DataType = 'Billing' AND Isactive = 1 AND DataSource = 'OData'",
                    "dbDW"
                );

                _url += $"?$filter=CreationDate ge datetime'{LastDate:yyyy-MM-dd}T00:00:00' and CreationDate le datetime'{Getdate:yyyy-MM-dd}T23:59:59'";

                // 2. ดึงข้อมูลจาก OData ทั้งหมดมาพักไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);
                if (dt.Rows.Count == 0) return;

                var subTasks = new List<Task>();

                foreach (DataRow row in dt.Rows)
                {
                    string billingDoc = row["BillingDocument"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL Logic (IF NOT EXISTS) เพื่อลดการยิง GetDataTable เช็คทีละแถว
                    batchSql.AppendLine($@"
                IF NOT EXISTS (SELECT 1 FROM Op_Billing WHERE BillingDocument = '{billingDoc}')
                BEGIN
                    INSERT INTO Op_Billing (
                        [BillingDocument],[BillingDocumentType],[BillingDocumentDate],[SoldToParty],[PayerParty],
                        [CompanyCode],[SalesOrganization],[DistributionChannel],[Division],[TransactionCurrency],
                        AbsltAccountingExchangeRate,[TotalNetAmount],[TaxAmount],[TotalGrossAmount],
                        [CustomerPaymentTerms],[OverallBillingStatus],[BillingDocumentIsCancelled],
                        [CancelledBillingDocument],[AccountingDocument],[CreationDate]
                    ) VALUES (
                        '{billingDoc}', '{row["BillingDocumentType"]}', '{row["BillingDocumentDate"]}', '{row["SoldToParty"]}', '{row["PayerParty"]}', 
                        '{row["CompanyCode"]}', '{row["SalesOrganization"]}', '{row["DistributionChannel"]}', '{row["Division"]}', '{row["TransactionCurrency"]}', 
                        '{row["AbsltAccountingExchangeRate"]}', '{row["TotalNetAmount"]}', '{row["TaxAmount"]}', '{row["TotalGrossAmount"]}', 
                        '{row["CustomerPaymentTerms"]}', '{row["OverallBillingStatus"]}', '{row["BillingDocumentIsCancelled"]}', 
                        '{row["CancelledBillingDocument"]}', '{row["AccountingDocument"]}', '{row["CreationDate"]}'
                    );
                END");

                    // 4. เตรียม Task สำหรับดึง Item ของ Billing นี้แบบขนาน
                    subTasks.Add(BillingDocumentItem(billingDoc, "Add"));

                    // ควบคุม Batch Size เพื่อไม่ให้ SQL Script ยาวเกินไป (ส่งทีละ 50 Billing)
                    if (subTasks.Count >= 50)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                        await Task.WhenAll(subTasks); // รันงานดึง Item พร้อมกัน
                        subTasks.Clear();
                    }
                }

                // เก็บตกรายการที่เหลือ
                if (batchSql.Length > 0) SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                if (subTasks.Count > 0) await Task.WhenAll(subTasks);

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('Add BillingHeader','Successful',GETDATE());", "dbDW");
                Console.WriteLine("AddBilling : Sync Successful");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('Add BillingHeader','Fail',GETDATE(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


        private static async Task UpdateBilling(string sText)
        {
            var batchSql = new StringBuilder();
            string _url = "";
            DateTime Getdate = DateTime.Today;
            DateTime LastDate = DateTime.Today.AddDays(-3);

            try
            {
                var helper = new ODataHelper();
                _url = SQLConnect.GetStringValue(
                    "SELECT [DataSyntax] FROM [Setting_SyncData] WHERE DataType = 'Billing' AND Isactive = 1 AND DataSource = 'OData'",
                    "dbDW"
                );

                // ดึงเฉพาะข้อมูลที่มีการเปลี่ยนแปลง (LastChangeDateTime)
                _url += $"?$filter=LastChangeDateTime ge datetimeoffset'{LastDate:yyyy-MM-dd}T00:00:00Z' and LastChangeDateTime le datetimeoffset'{Getdate:yyyy-MM-dd}T23:59:59Z'";

                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);
                if (dt.Rows.Count == 0) return;

                var subTasks = new List<Task>();

                foreach (DataRow row in dt.Rows)
                {
                    string billingDoc = row["BillingDocument"].ToString().Replace("'", "''");

                    // 1. ใช้ SQL Logic (IF EXISTS) เพื่อรวมคำสั่งเช็คและอัปเดตใน Script เดียว
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Op_Billing WHERE BillingDocument = '{billingDoc}')
                BEGIN
                    UPDATE Op_Billing SET 
                        BillingDocumentType = '{row["BillingDocumentType"]}', SoldToParty = '{row["SoldToParty"]}',
                        PayerParty = '{row["PayerParty"]}', CompanyCode = '{row["CompanyCode"]}',
                        BillingDocumentDate = '{row["BillingDocumentDate"]}', SalesOrganization = '{row["SalesOrganization"]}',
                        DistributionChannel = '{row["DistributionChannel"]}', Division = '{row["Division"]}',
                        TransactionCurrency = '{row["TransactionCurrency"]}', AbsltAccountingExchangeRate = '{row["AbsltAccountingExchangeRate"]}',
                        TotalNetAmount = '{row["TotalNetAmount"]}', TaxAmount = '{row["TaxAmount"]}',
                        CustomerPaymentTerms = '{row["CustomerPaymentTerms"]}', OverallBillingStatus = '{row["OverallBillingStatus"]}',
                        BillingDocumentIsCancelled = '{row["BillingDocumentIsCancelled"]}', CancelledBillingDocument = '{row["CancelledBillingDocument"]}',
                        AccountingDocument = '{row["AccountingDocument"]}', CreationDate = '{row["CreationDate"]}'
                    WHERE BillingDocument = '{billingDoc}';
                END");

                    // 2. เตรียม Task สำหรับอัปเดต Item ของ Billing แบบ Parallel
                    subTasks.Add(BillingDocumentItem(billingDoc, "Update"));

                    // 3. ควบคุม Batch Size (ทุกๆ 50 รายการ) เพื่อไม่ให้ Transaction ใหญ่เกินไป
                    if (subTasks.Count >= 50)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                        await Task.WhenAll(subTasks); // รันงาน Item พร้อมกัน
                        subTasks.Clear();
                    }
                }

                // เก็บตกรายการที่เหลือ
                if (batchSql.Length > 0) SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                if (subTasks.Count > 0) await Task.WhenAll(subTasks);

                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('Update BillingHeader','Successful',GETDATE());", "dbDW");
                Console.WriteLine("UpdateBilling : Sync Successful");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('Update BillingHeader','Fail',GETDATE(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task BillingDocumentItem(string BillingDoc, string BillinngType)
        {
            var batchSql = new StringBuilder();
            try
            {
                var helper = new ODataHelper();
                string _url = SQLConnect.GetStringValue(
                    "SELECT [DataSyntax] FROM [Setting_SyncData] WHERE DataType = 'BillingDocumentItem' AND IsActive = 1 AND DataSource = 'OData'",
                    "dbDW");

                string filter = $"BillingDocument eq '{BillingDoc}'";
                string url = $"{_url}?$filter={Uri.EscapeDataString(filter)}&$format=json";

                DataTable dt = await helper.FetchODataSinglePageAsync(url, _USERNAME, _PASSWORD);
                if (dt == null || dt.Rows.Count == 0) return;

                // 1. ถ้าเป็น Update ให้ลบทิ้งก่อน (Logic เดิมของคุณ)
                if (BillinngType == "Update")
                {
                    SQLConnect.Updatedata($"DELETE FROM Op_BillingDocumentItem WHERE BillingDocument = '{BillingDoc.Replace("'", "''")}'", "dbDW");
                }

                foreach (DataRow row in dt.Rows)
                {
                    string bDoc = row["BillingDocument"].ToString().Replace("'", "''");
                    string bItem = row["BillingDocumentItem"].ToString().Replace("'", "''");

                    // 2. จัดการ Format วันที่ ป้องกัน Out-of-range
                    string pricingDate = DateTime.TryParse(row["PricingDate"]?.ToString(), out DateTime pDate)
                        ? $"'{pDate:yyyyMMdd HH:mm:ss}'" : "NULL";
                    string creationDate = DateTime.TryParse(row["CreationDate"]?.ToString(), out DateTime cDate)
                        ? $"'{cDate:yyyyMMdd HH:mm:ss}'" : "NULL";

                    // 3. ใช้ IF NOT EXISTS เพื่อกัน PK Duplicate
                    batchSql.AppendLine($@"
            IF NOT EXISTS (SELECT 1 FROM Op_BillingDocumentItem WHERE BillingDocument = '{bDoc}' AND BillingDocumentItem = '{bItem}')
            BEGIN
                INSERT INTO Op_BillingDocumentItem (
                    [BillingDocument],[BillingDocumentItem],[SalesDocument],[SalesDocumentItem],[ReferenceSDDocument],
                    [ReferenceSDDocumentItem],[ReferenceSDDocumentCategory],[SalesGroup],[SalesOffice],[Division],
                    [Material],[OriginallyRequestedMaterial],[InternationalArticleNumber],[BillingDocumentItemText],
                    [Plant],[StorageLocation],[Batch],[BillingQuantity],[BillingQuantityUnit],[TransactionCurrency],
                    [NetAmount],[CostAmount],[GrossAmount],[TaxAmount],[Subtotal6Amount],[PricingDate],[ProfitCenter],
                    [CostCenter],[SalesSDDocumentCategory],[CreationDate],[CreatedByUser],
                    AdditionalCustomerGroup1, AdditionalCustomerGroup2, AdditionalCustomerGroup3, 
                    AdditionalCustomerGroup4, AdditionalCustomerGroup5, AdditionalMaterialGroup1, AdditionalMaterialGroup2
                ) VALUES (
                    '{bDoc}', '{bItem}', '{row["SalesDocument"]}', '{row["SalesDocumentItem"]}', '{row["ReferenceSDDocument"]}',
                    '{row["ReferenceSDDocumentItem"]}', '{row["ReferenceSDDocumentCategory"]}', '{row["SalesGroup"]}', '{row["SalesOffice"]}', '{row["Division"]}',
                    '{row["Material"]}', '{row["OriginallyRequestedMaterial"]}', '{row["InternationalArticleNumber"]}', 
                    '{row["BillingDocumentItemText"].ToString().Replace("'", "''").Replace(",", "")}', 
                    '{row["Plant"]}', '{row["StorageLocation"]}', '{row["Batch"].ToString().Replace("'", "''")}', 
                    '{row["BillingQuantity"]}', '{row["BillingQuantityUnit"]}', '{row["TransactionCurrency"]}', 
                    '{row["NetAmount"]}', '{row["CostAmount"]}', '{row["GrossAmount"]}', '{row["TaxAmount"]}', 
                    '{row["Subtotal6Amount"]}', {pricingDate}, '{row["ProfitCenter"]}', '{row["CostCenter"]}', 
                    '{row["SalesSDDocumentCategory"]}', {creationDate}, '{row["CreatedByUser"]}',
                    '{row["AdditionalCustomerGroup1"]}', '{row["AdditionalCustomerGroup2"]}', '{row["AdditionalCustomerGroup3"]}', 
                    '{row["AdditionalCustomerGroup4"]}', '{row["AdditionalCustomerGroup5"]}', '{row["AdditionalMaterialGroup1"]}', 
                    '{row["AdditionalMaterialGroup2"]}'
                );
            END");
                }

                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }
                Console.WriteLine($"BillingDocumentItem {BillingDoc} : Sync Successful");
            }
            catch (Exception ex)
            {
                // บันทึก Log แบบป้องกัน LogDescription เต็ม (ใช้ NVARCHAR(MAX))
                string errMsg = ex.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('BillingItem','Fail',GETDATE(),'{errMsg}');", "dbDW");
                Console.WriteLine($"Error on {BillingDoc}: {ex.Message}");
            }
        }

        private static async Task AddOutbDelivery(string sText)
        {
            var batchSql = new StringBuilder();
            string _url = "";
            int rowCount = 0;
            DateTime Getdate = DateTime.Today;
            DateTime LastDate = DateTime.Today.AddDays(-3);

            try
            {
                var helper = new ODataHelper();

                // 1. ดึง URL การตั้งค่าครั้งเดียวจากฐานข้อมูล
                _url = SQLConnect.GetStringValue(
                    "SELECT [DataSyntax] FROM [Setting_SyncData] WHERE DataType = 'OutbDelivery' AND Isactive = 1 AND DataSource = 'OData'",
                    "dbDW"
                );

                _url += $"?$filter=CreationDate ge datetime'{LastDate:yyyy-MM-dd}T00:00:00' and CreationDate le datetime'{Getdate:yyyy-MM-dd}T23:59:59'";

                // 2. ดึงข้อมูลจาก OData API ทั้งหมดมาพักไว้ใน DataTable
                DataTable dt = await helper.FetchAllODataAsync(_url, _USERNAME, _PASSWORD);
                if (dt.Rows.Count == 0) return;

                foreach (DataRow row in dt.Rows)
                {
                    string deliveryDoc = row["DeliveryDocument"].ToString().Replace("'", "''");

                    // 3. ใช้ SQL Logic (UPSERT) เพื่อลดการยิง Query เช็คทีละแถวจากฝั่ง C#
                    batchSql.AppendLine($@"
                IF EXISTS (SELECT 1 FROM Op_OutbDelivery WHERE DeliveryDocument = '{deliveryDoc}')
                BEGIN
                    UPDATE Op_OutbDelivery SET 
                        DocumentDate = '{row["DocumentDate"]}', DeliveryDate = '{row["DeliveryDate"]}',
                        PickingDate = '{row["PickingDate"]}', PlannedGoodsIssueDate = '{row["PlannedGoodsIssueDate"]}',
                        HeaderGrossWeight = '{row["HeaderGrossWeight"]}', HeaderNetWeight = '{row["HeaderNetWeight"]}',
                        OvrlItmPickingIncompletionSts = '{row["OvrlItmPickingIncompletionSts"]}', 
                        UpdateDate = GETDATE()
                    WHERE DeliveryDocument = '{deliveryDoc}';
                END
                ELSE
                BEGIN
                    INSERT INTO Op_OutbDelivery (
                        [DeliveryDocument],[DocumentDate],[DeliveryDate],[PickingDate],
                        [PlannedGoodsIssueDate],[HeaderGrossWeight],[HeaderNetWeight],
                        [OvrlItmPickingIncompletionSts],[UpdateDate]
                    ) VALUES (
                        '{deliveryDoc}', '{row["DocumentDate"]}', '{row["DeliveryDate"]}', '{row["PickingDate"]}', 
                        '{row["PlannedGoodsIssueDate"]}', '{row["HeaderGrossWeight"]}', '{row["HeaderNetWeight"]}', 
                        '{row["OvrlItmPickingIncompletionSts"]}', GETDATE()
                    );
                END");

                    rowCount++;

                    // 4. ส่งข้อมูลเข้าฐานข้อมูลเป็นชุด (Batch) ชุดละ 100 แถว เพื่อความเร็วสูงสุด
                    if (rowCount % 100 == 0)
                    {
                        SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                        batchSql.Clear();
                    }
                }

                // เก็บตกรายการที่เหลือในชุดสุดท้าย
                if (batchSql.Length > 0)
                {
                    SQLConnect.Updatedata(batchSql.ToString(), "dbDW");
                }

                // บันทึก Log เมื่อทำงานสำเร็จ
                SQLConnect.Updatedata("INSERT INTO Log_Status(Process,Status,LogDate) VALUES ('Add OutbDelivery','Successful',GETDATE());", "dbDW");
                Console.WriteLine($"AddOutbDelivery : Sync Successful ({rowCount} rows)");
            }
            catch (Exception e)
            {
                string errMsg = e.Message.Replace("'", "''");
                SQLConnect.Updatedata($"INSERT INTO Log_Status(Process,Status,LogDate,LogDescription) VALUES ('Add OutbDelivery','Fail',GETDATE(),'{errMsg}');", "dbDW");
                Console.WriteLine(e.Message);
            }
        }


    }
}
