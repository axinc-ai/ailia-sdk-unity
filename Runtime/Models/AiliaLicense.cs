using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ailia {
public class AiliaLicense
{
    private const string LicenseServer = "axip-console.appspot.com";
    private const string LicenseApi = "/license/download/product/AILIA";

    private const string LicenseFileFormat = "--- shalo license file ---\naxell:ailia\n";
    private static bool displayLicenseWarning = true;

    private static void DownloadLicense(string licPath)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri($"https://{LicenseServer}");
            HttpResponseMessage response = httpClient.GetAsync(LicenseApi).Result;

            if (response.IsSuccessStatusCode)
            {
                byte[] licenseFile = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(licPath, licenseFile);
            }
            else
            {
                throw new Exception("License file download failed");
            }
        }
    }

    private static string CheckLicense(string licPath)
    {
        string userData = "";

        if (!File.Exists(licPath))
        {
            Debug.Log($"License file {licPath} is not found.");
            return null;
        }

        string licenseFileContent = File.ReadAllText(licPath);
        licenseFileContent = licenseFileContent.Replace("\r\n", "\n");
        if (!licenseFileContent.StartsWith(LicenseFileFormat))
        {
            Debug.Log($"License file {licPath} has invalid format.");
            return null;
        }

        string[] lines = licenseFileContent.Split('\n');
        Match match = Regex.Match(lines[2], @"(\d{4})/(\d{2})/(\d{2})");

        if (!match.Success)
        {
            Debug.Log($"License file {licPath} has invalid format.");
            return null;
        }

        DateTime expiryDate = new DateTime(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), 23, 59, 59);
        if (DateTime.Now > expiryDate)
        {
            Debug.Log($"License date of {licPath} has been expired.");
            return null;
        }

        userData = lines.Length > 3 ? lines[3] : "";

        return userData;
    }

    private static void DisplayWarning()
    {
        if (!displayLicenseWarning) return;

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Japanese:
                Debug.Log("ailiaへようこそ。ailia SDKは商用ライブラリです。特定の条件下では、無償使用いただけますが、原則として有償ソフトウェアです。詳細は https://ailia.ai/license/ を参照してください。");
                break;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
                Debug.Log("欢迎来到ailia。ailia SDK是商业库。在特定条件下，可以免费使用，但原则上是付费软件。详情请参阅 https://ailia.ai/license/ 。");
                break;
            default:
                Debug.Log("Welcome to ailia! The ailia SDK is a commercial library. Under certain conditions, it can be used free of charge; however, it is principally paid software. For details, please refer to https://ailia.ai/license/ .");
                break;
        }
        displayLicenseWarning = false;
    }

    public static void CheckAndDownloadLicense()
    {
        if (Marshal.PtrToStringAnsi(Ailia.ailiaGetVersion()).Contains("perpetual_license")){
            return; // not required license file
        }

        string userData = "";
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string currentDirectory = Environment.CurrentDirectory;
        string licFolder = currentDirectory;
#else
    #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string licFolder = Path.Combine(homePath, "Library/SHALO/");
    #else
        #if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string licFolder = Path.Combine(homePath, ".shalo/");
        #else
            return; // iOS and Android not required license file
        #endif
    #endif
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        string licFile = Path.Combine(licFolder, "AILIA.lic");

        userData = CheckLicense(licFile);
        if (userData == null)
        {
            Debug.Log("Downloading license file for ailia SDK.");
            DirectoryInfo di = Directory.CreateDirectory(licFolder);
            DownloadLicense(licFile);
            userData = CheckLicense(licFile);
        }

        if (userData == null){
            Debug.Log("Download license file failed.");
            return;
        }

        if (userData.Contains("trial version"))
        {
            DisplayWarning();
        }
#endif
    }
}
} // namespace ailia
