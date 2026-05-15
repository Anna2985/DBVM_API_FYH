using Basic;
using H_Pannel_lib;
using HIS_DB_Lib;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp_stock
{
    class Program
    {
        static void Main(string[] args)
        {
            string API = "http://127.0.0.1:4433";
            List<sys_serverSettingClass> settingClasses = sys_serverSettingClass.get_name(API);
            List<medClass> med_cloud = medClass.get_med_cloud(API);
            Dictionary<string, List<medClass>> medCloudDict = medClass.CoverToDictionaryByCode(med_cloud);

            foreach (var item in settingClasses)
            {
                string serverName = item.設備名稱;
                string serverType = item.類別;
                string url = $"{API}/api/device/list/{serverName}";
                string result = Basic.Net.WEBApiGet(url);
                List<string> code_stock = new List<string>();
                returnData returnData = result.JsonDeserializet<returnData>();
                List<DeviceBasic> deviceBasics = returnData.Data.ObjToClass<List<DeviceBasic>>();
                List<stockClass> add = new List<stockClass>();
                List<stockClass> update = new List<stockClass>();

                returnData returnData_stock = stockClass.get_stock(API, serverName, serverType);
                List<stockClass> stockClasses = returnData_stock.Data.ObjToClass<List<stockClass>>();
                Dictionary<string, List<stockClass>> dic_stock = stockClasses.ToDictByCode();

                for (int i = 0; i < deviceBasics.Count; i++)
                {
                    string 藥碼 = deviceBasics[i].BarCode;
                    stockClass stocks = dic_stock.GetByCode(藥碼).FirstOrDefault();
                    code_stock.Add(藥碼);
                    if (stocks == null)
                    {
                        stockClass stockClass = new stockClass();
                        stockClass.藥碼 = 藥碼;
                        //stockClass.料號 = medlasses[0].料號;
                        //stockClass.藥名 = medlasses[0].藥品名稱;
                        stockClass.效期 = deviceBasics[i].List_Validity_period;
                        stockClass.批號 = deviceBasics[i].List_Lot_number;
                        stockClass.數量 = deviceBasics[i].List_Inventory;
                        add.Add(stockClass);
                    }
                    else
                    {
                        //stocks.料號 = medlasses[0].料號;
                        //stocks.藥名 = medlasses[0].藥品名稱;
                        stocks.效期 = deviceBasics[i].List_Validity_period;
                        stocks.批號 = deviceBasics[i].List_Lot_number;
                        stocks.數量 = deviceBasics[i].List_Inventory;
                        update.Add(stocks);
                    }
                }
                if (add.Count > 0)
                {
                    returnData returnData_add = stockClass.add(API, serverName, serverType, add);
                    if (returnData_add == null) Logger.Log(add.JsonSerializationt(true));
                    else Logger.Log($"{returnData_add.JsonSerializationt(true)}");

                }
                if (update.Count > 0)
                {
                    returnData returnData_update = stockClass.update(API, serverName, serverType, update);
                    if (returnData_update == null) Logger.Log(update.JsonSerializationt(true));
                    else Logger.Log($"{returnData_update.JsonSerializationt(true)}");

                }
            }           
        }
    }
}
