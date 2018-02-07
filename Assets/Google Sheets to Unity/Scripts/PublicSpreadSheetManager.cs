using Google.GData.Client;
using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using GoogleSheetsToUnity;
using System.Collections;
using GoogleSheetsToUnity.ThirdPary;

#if UNITY_EDITOR
using UnityEditor;
#endif

public delegate void OnSpreedSheetLoaded();
namespace GoogleSheetsToUnity
{
    public class PublicSpreadSheetManager
    {
        GoogleSheetsToUnityConfig _config;
        private GoogleSheetsToUnityConfig config
        {
            get
            {
                if (_config == null)
                {
                    _config = (GoogleSheetsToUnityConfig)Resources.Load("GSTU_Config");
                }

                return _config;
            }
            set
            {
                _config = value;
            }
        }

        public event OnSpreedSheetLoaded onFinishedLoading;

        string defaultA1Notation = "A1:I100";

        public List<string> titles = new List<string>();
        public Dictionary<string, Row> WorkSheetData = new Dictionary<string, Row>();

        public class Row
        {
            public Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
        }

        public void LoadPublicWorksheet(string spreadsheetID, OnSpreedSheetLoaded callback = null)
        {
            WWW www = new WWW("https://sheets.googleapis.com/v4/spreadsheets/" + spreadsheetID + "/values/" + defaultA1Notation + "?key=" + config.API_Key);
            Load(www, callback);
        }

        public void LoadPublicWorksheet(string spreadsheetID, string worksheetName, OnSpreedSheetLoaded callback = null)
        {
            WWW www = new WWW("https://sheets.googleapis.com/v4/spreadsheets/" + spreadsheetID + "/values/" + worksheetName + "!" + defaultA1Notation + "?key=" + config.API_Key);
            Load(www, callback);
        }

        public void LoadPublicWorksheet(string spreadsheetID, string worksheetName, string from, string to, OnSpreedSheetLoaded callback = null)
        {
            WWW www = new WWW("https://sheets.googleapis.com/v4/spreadsheets/" + spreadsheetID + "/values/" + worksheetName + "!" + from + ":" + to + "?key=" + config.API_Key);
            Load(www, callback);
        }

        public void LoadPublicWorksheet(string spreadsheetID, string from, string to, OnSpreedSheetLoaded callback = null)
        {
            WWW www = new WWW("https://sheets.googleapis.com/v4/spreadsheets/" + spreadsheetID + "/values/" + from + ":" + to + "?key=" + config.API_Key);
            Load(www, callback);
        }

        public void Load(WWW url, OnSpreedSheetLoaded callback)
        {
            if(string.IsNullOrEmpty(config.API_Key))
            {
                Debug.Log("Missing API Key, please enter this in the confie settings");
                return;
            }

            if (callback != null)
            {
                onFinishedLoading += callback;
            }

            if(Application.isPlaying)
            {
                Task t = new Task(WaitForRequest(url));
            }

#if UNITY_EDITOR
            else
            {
                EditorCoroutineRunner.StartCoroutine(WaitForRequest(url));
            }
#endif
        }


        private IEnumerator WaitForRequest(WWW www)
        {
            yield return www;

            ProcessJSONData(www.text);
        }

        private void ProcessJSONData(string json)
        {
            var dict = Json.Deserialize(json) as Dictionary<string, object>;

            var d = dict["values"] as List<object>;

            bool isTitles = true;
            string lastSheetID = "";
            foreach (List<object> rowData in d)
            {
                if (isTitles)
                {
                    for (int i = 0; i < rowData.Count; i++)
                    {
                        titles.Add(rowData[i] as string);
                    }

                    isTitles = false;
                }
                else
                {
                    if (rowData[0] as string == "")
                    {
                        for (int i = 1; i < rowData.Count; i++)
                        {
                            if (rowData[i] as string != "")
                            {
                                if (!WorkSheetData[lastSheetID].data.ContainsKey(titles[i]))
                                {
                                    WorkSheetData[lastSheetID].data.Add(titles[i], new List<string>());
                                }
                                WorkSheetData[lastSheetID].data[titles[i]].Add(rowData[i].ToString());
                            }
                        }
                    }
                    else
                    {
                        lastSheetID = rowData[0].ToString();
                        WorkSheetData.Add(lastSheetID, new Row());

                        for (int i = 1; i < rowData.Count; i++)
                        {
                            if (rowData[i] as string != "")
                            {
                                if (!WorkSheetData[lastSheetID].data.ContainsKey(titles[i]))
                                {
                                    WorkSheetData[lastSheetID].data.Add(titles[i], new List<string>());
                                }
                                WorkSheetData[lastSheetID].data[titles[i]].Add(rowData[i].ToString());
                            }
                        }
                    }
                }
            }

            if (onFinishedLoading != null)
            {
                onFinishedLoading();
            }
        }

        #region V3
        //row titles are stored on
        public int titleRow
        {
            get
            {
                return titleRowActual + 2;
            }
            set
            {
                titleRowActual = value - 2;
            }
        }

        int titleRowActual;

        /// <summary>
        /// Loads a public spreadsheet and worksheet(worksheets start at 1 not 0)
        /// </summary>
        /// <param name="spreadsheetID"></param>
        /// <param name="worksheetNumber"></param>
        /// <returns></returns>
        [Obsolete("This method is now outdated and will be removed in a v1.0 update, Use other variations")]
        public WorksheetData LoadPublicWorkSheet(string spreadsheetID, int worksheetNumber)
        {
            SecurityPolicy.Instate();

            SpreadsheetsService publicService = new SpreadsheetsService("Unity");

            ListQuery listQuery = new ListQuery("https://spreadsheets.google.com/feeds/list/" + spreadsheetID + "/" + worksheetNumber + "/public/values");

            ListFeed feed = publicService.Query(listQuery) as ListFeed;
            WorksheetData returnData = new WorksheetData();

            List<string> titles = GetColumnTitles(feed);

            if (titleRowActual > 0)
            {
                //remove all rows above the title row
                for (int i = 0; i <= titleRowActual; i++)
                {
                    feed.Entries.RemoveAt(0);
                }
            }

            foreach (ListEntry row in feed.Entries)
            {
                string rowTitle = row.Title.Text;
                RowData rowData = new RowData(rowTitle);

                int rowId = 0;
                foreach (ListEntry.Custom element in row.Elements)
                {
                    rowData.cells.Add(new CellData(element.Value, titles[rowId], rowTitle));
                    rowId++;
                }

                returnData.rows.Add(rowData);
            }

            return returnData;
        }

        List<string> GetColumnTitles(ListFeed feed)
        {
            List<string> titles = new List<string>();

            if (titleRowActual < 0)
            {
                ListEntry row = feed.Entries[0] as ListEntry;

                foreach (ListEntry.Custom element in row.Elements)
                {
                    titles.Add(element.LocalName);
                }
            }
            else
            {
                ListEntry row = feed.Entries[titleRowActual] as ListEntry;

                foreach (ListEntry.Custom element in row.Elements)
                {
                    titles.Add(element.Value);
                }
            }

            return titles;
        }
        #endregion //Remove in future update Will be removed in future release
    }
}
