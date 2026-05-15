using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using HIS_DB_Lib;
using Basic;
using Newtonsoft.Json;

namespace DB2VM_API
{
    public class orderlistClass
    {
        [JsonPropertyName("opdDate")]
        public string 開方日期 { get; set; }
        [JsonPropertyName("divisionName")]
        public string 科別 { get; set; }
        [JsonPropertyName("doctorName")]
        public string 醫生姓名 { get; set; }
        [JsonPropertyName("patientNo")]
        public int 病歷號 { get; set; }
        [JsonPropertyName("medicationOrderNumber")]
        public int 領藥號 { get; set; }
        [JsonPropertyName("patientName")]
        public string 病人姓名 { get; set; }
        [JsonPropertyName("age")]
        public double 年齡 { get; set; }
        [JsonPropertyName("nsName")]
        public string 病房 { get; set; }
        [JsonPropertyName("bedNo")]
        public string 床號 { get; set; }
        [JsonPropertyName("orderDate")]
        public string orderDate { get; set; }
        [JsonPropertyName("sheetNo")]
        public string 藥袋條碼 { get; set; }
        [JsonPropertyName("class")]
        public string 藥袋類型 { get; set; }
        [JsonPropertyName("allergicNote")]
        public string 過敏備註 { get; set; }
        [JsonPropertyName("medicationItems")]
        public List<MedicationOrder> medicationItems { get; set; }
        public List<diagnosisItems> diagnosisItems { get; set; }
        public List<allergicList> allergicList { get; set; }
        public List<interactionsList> interactionsList { get; set; }

        static public orderlistClass get_order(string Barcode)
        {
            string url = "";
            orderlistClass orderlistClass = new orderlistClass();
            string json_out = "";
            if (Barcode.Length > 12)
            {
                //門診、急診 條碼B1140131000376634 01
                //管制藥櫃條碼 B1140123000736092 00#
                Barcode = Barcode.Replace("#", "%23#");
                url = $"http://192.168.16.230:8132/api/medication/scanclinicmedicationbag?barcode={Barcode}";
                json_out = Net.WEBApiGet(url);
                orderlistClass = json_out.JsonDeserializet<orderlistClass>();
                if (orderlistClass == null) return null;
                if (Barcode.Contains("#")) 
                {
                    orderlistClass.藥袋類型 = "管制藥";           
                }
                else
                {
                    orderlistClass.藥袋類型 = "門診/急診";
                }
            }
            else
            {
                //住院條碼 A11406485048
                url = $"http://192.168.16.230:8132/api/Medication/ScanInpMedicationBag?BarCode={Barcode}";
                json_out = Net.WEBApiGet(url);
                orderlistClass = json_out.JsonDeserializet<orderlistClass>();
                orderlistClass.藥袋類型 = "住院";
            }
            returnData returnData = new returnData();
            
            if(orderlistClass.medicationItems.Count == 0)
            {
                ///出院帶藥
                url = $"http://192.168.16.230:8132/api/Medication/ScanDisHomeMedicationBag?BarCode={Barcode}";
                returnData = new returnData();
                json_out = Net.WEBApiGet(url);
                orderlistClass = json_out.JsonDeserializet<orderlistClass>();
                orderlistClass.藥袋類型 = "出院帶藥";
            }
            return orderlistClass;
        }
        static public List<orderlistClass> get_order_by_date(string date)
        {
            string url = $"http://192.168.16.230:8132/api/Medication/GetInpControlOrderList?Date={date}";
            returnData returnData = new returnData();
            string json_out = Net.WEBApiGet(url);
            List<orderlistClass> orderlistClasses = json_out.JsonDeserializet<List<orderlistClass>>();
            return orderlistClasses;
        }

    }
    public class MedicationOrder
    {
        [JsonPropertyName("code")]
        public string 料號 { get; set; }
        [JsonPropertyName("fullName")]
        public string 藥品名稱 { get; set; }
        [JsonPropertyName("qty")]
        public double 單次劑量 { get; set; }
        [JsonPropertyName("useName")]
        public string 頻次 { get; set; }
        [JsonPropertyName("medthodName")]
        public string 途徑 { get; set; }
        [JsonPropertyName("tqty")]
        public double 交易量 { get; set; }
        [JsonPropertyName("acntPtr")]
        public int 批序 { get; set; }
        [JsonPropertyName("type")]
        public string 藥袋狀態 { get; set; }
        [JsonPropertyName("stockCode")]
        public string 藥品碼 { get; set; }
        public class ICP_By_seq : IComparer<OrderClass>
        {
            public int Compare(OrderClass x, OrderClass y)
            {
                return (x.批序.StringToInt32()).CompareTo(y.批序.StringToInt32());
            }
        }
    }
    public class diagnosisItems
    {
        public string diagnosisCode { get; set; }
        public string diagnosisName { get; set; }
    }
    public class allergicList
    {
        public string code { get; set; }
        public string fullName { get; set; }
        public string stockCode { get; set; }

    }
    public class interactionsList
    {
        public string code { get; set; }
        public string fullName { get; set; }
        public string stockCode { get; set; }

    }

}
