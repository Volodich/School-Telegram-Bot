using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BalTelegramBot.Models.Commands.Scheduler;

namespace BalTelegramBot.Controllers
{
    public class GoogleSpreadsheetController
    {
        // GS - Google Spreadsheet
        #region System Property
        private static string PuthToGSjsonFile => @"wwwroot";
        private static string NameGSjsonFile => "VolodichGoogleSheetsApiKey.json";
        public static string ApplicationName => "BalTelegramBot";

        #endregion

        internal static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static SheetsService _sheetsService;
        private static string SpreadsheetId => "1kP7VdHosIcpXrQ4D4iKcj1Bh-nH4WfJPkTyM4r-HCcs";

        public static async Task<bool> ConnectToSheetsAsync()
        {
            try
            {
                GoogleCredential credential;
                await Task.Run(() =>
                {
                    using (var strean = new FileStream(Path.Combine(PuthToGSjsonFile, NameGSjsonFile),
                        FileMode.Open,
                        FileAccess.Read))
                    {
                        credential = GoogleCredential.FromStream(strean).CreateScoped(Scopes);
                    }
                    _sheetsService = new SheetsService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });
                });
            }
            catch(Exception)
            {
                return false;
            }
            return true;
        }

        public abstract class SchedulerSheet
        {
            public abstract string NameTable { get; }
            public abstract int RowCount { get; set; }
            public abstract int ColumnCount { get; set; }
            public abstract int SheetIndex { get; }

            internal virtual void GetSheetData() // Get All system Data
            {
                SpreadsheetsResource.GetRequest request = _sheetsService.Spreadsheets.Get(SpreadsheetId);
                IList<Sheet> sheets = request.Execute().Sheets;

                RowCount = sheets[SheetIndex].Properties.GridProperties.RowCount.Value; // Max count
                ColumnCount = sheets[SheetIndex].Properties.GridProperties.ColumnCount.Value; // Max count
            }

            internal virtual async Task GetSheetDataAsync()
            {
                await Task.Run(GetSheetData);
            }

            internal async Task<IList<IList<object>>> GetScheduler()
            {
                string range = $"{NameTable}!A1:Q{RowCount}";

                var scheduler = await _sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range).ExecuteAsync();
                return scheduler.Values;
            }

        }

        

        public class SchedulerPupil : SchedulerSheet
        {
            public override string NameTable => new Scheduler.Pupil().NameTable;
            public override int RowCount { get; set; }
            public override int ColumnCount { get; set; }
            public override int SheetIndex => 0;

            public SchedulerPupil()
            {
                ConnectToSheetsAsync().Wait();
            }
        }

        public class SchedulerTeacher : SchedulerSheet
        {
            public override string NameTable => new Scheduler.Teacher().NameTable;
            public override int RowCount { get; set; }
            public override int ColumnCount { get; set; }
            public override int SheetIndex => 1;
            public SchedulerTeacher()
            {
                ConnectToSheetsAsync().Wait(); 
            }

        }

        public class TeacherInformation : SchedulerSheet
        {
            public override string NameTable => "TeacherInformation";
            public override int RowCount { get; set; }
            public override int ColumnCount { get; set; }
            public override int SheetIndex => 2;

            internal async Task<IList<IList<object>>> GetTeacherInformation()
            {
                string range = $"{NameTable}!A1:Q{RowCount}";

                var scheduler = await _sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range).ExecuteAsync();
                return scheduler.Values;
            }

        }

        public class Holidays : SchedulerSheet
        {
            public override string NameTable => "Свята";
            public override int RowCount { get; set; }
            public override int ColumnCount { get; set; }
            public override int SheetIndex => 3;

            public Holidays()
            {
                ConnectToSheetsAsync().Wait();
            }

            internal async Task<IList<IList<object>>> GetHolidaysAsync()
            {
                string range = $"{NameTable}!A1:Q{RowCount}";

                var scheduler = await _sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range).ExecuteAsync();
                return scheduler.Values;
            }
        }

        /*
         1. Logic to connect gs and get data.
         2. CRUD from gs
         3. CRUD from db table
         */
    }
}
