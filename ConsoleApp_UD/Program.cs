using System;
using System.Threading;
using System.Threading.Tasks;
using Basic;
using HIS_DB_Lib;
using System.ComponentModel;

namespace ConsoleApp_UD
{
    class Program
    {
        

        private static readonly string MutexName = "ConsoleApp_UD";
        private static readonly object logLock = new object();

        static public DateTime dateTime = DateTime.Now.AddDays(0);

        static async Task Main(string[] args)
        {
            //if (System.Diagnostics.Debugger.IsAttached)
            //{
            //    SafeLog("偵錯模式啟動，按 Enter 後開始執行...");
            //    Console.ReadLine();
            //}
            Console.Title = MutexName;
            using (Mutex mutex = new Mutex(false, MutexName, out bool isNewInstance))
            {
                if (!isNewInstance)
                {
                    SafeLog("偵錯：程式已在執行，避免重複啟動");
                    return;
                }

                
                    try
                    {
                        Console.WriteLine("取得order開始");
                      
                        string apiUrl = Basic.Net.WEBApiGet("http://192.168.12.164:4434/api/UD");
                        Console.WriteLine($"取得order結束 \n {apiUrl}");
                        Console.WriteLine("寫入UD開始");

                        string UD = Basic.Net.WEBApiGet("http://192.168.12.164:4434/api/med_Cart/GetUD_Data");
                        Console.WriteLine($"取得order結束 \n {UD}");

                    }
                    catch (Exception ex)
                    {
                        SafeLog("例外: " + ex.Message);
                    }

                    //if (System.Diagnostics.Debugger.IsAttached)
                    //{
                    //    SafeLog("偵錯模式：按 Enter 繼續，ESC 結束");
                    //    var key = Console.ReadKey();
                    //    if (key.Key == ConsoleKey.Escape) break;
                    //    Console.WriteLine();
                    //}
                    //else
                    //{
                    //    break;
                    //}

                

                SafeLog("程式結束");
            }
        }

        static async Task<string> CallApiWithDots(string url, string json_in)
        {
            var task = Task.Run(() => Basic.Net.WEBApiPostJson(url, json_in));

            int dotCount = 0;
            const int maxDots = 5;

            while (!task.IsCompleted)
            {
                dotCount = (dotCount % maxDots) + 1;
                string dots = new string('.', dotCount);

                Console.Write($"\r等待中 {dots}   ");
                await Task.Delay(500);
            }

            Console.Write("\r                             \r"); // 清行
            return task.Result;
        }

        static string GetTodayRocDate()
        {
            DateTime today = dateTime;
            int rocYear = today.Year - 1911;
            return $"{rocYear:000}{today:MMdd}";
        }

        static void SafeLog(string msg)
        {
            lock (logLock)
            {
                Logger.Log(msg);
            }
        }
    }
}
