using UnityEngine;
using System.Collections;

namespace GoogleSheetsToUnity
{
    public class GoogleSheetsToUnityConfig : ScriptableObject
  {
    public string CLIENT_ID = "";
    public string CLIENT_SECRET = "";
    public string ACCESS_TOKEN = "";

    [HideInInspector]
    public string REFRESH_TOKEN;

    public string API_Key = "";
    }
}