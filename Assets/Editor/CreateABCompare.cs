using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CreateABCompare
{
    //[MenuItem("AB包工具/创建对比文件")]
    public static void CreateABCompareFile()
    {
        CleanInvalidMetaFiles(Application.dataPath + "/ArtRes/AB/PC/");

        //获取文件夹信息
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/ArtRes/AB/PC/");
        FileInfo[] fileInfos = directory.GetFiles();

        string abCompareInfo = "";

        foreach (FileInfo info in fileInfos)
        {
            if (info.Extension == "")
            {
                abCompareInfo += info.Name + " " + info.Length + " " + GetMD5(info.FullName);
                abCompareInfo += '|';
            }
        }

        if (abCompareInfo.Length > 0)
        {
            abCompareInfo = abCompareInfo.Substring(0, abCompareInfo.Length - 1);
        }

        File.WriteAllText(Application.dataPath + "/ArtRes/AB/PC/ABCompareInfo.txt", abCompareInfo);
        AssetDatabase.Refresh();

        Debug.Log("AB包对比文件生成成功");
    }

    public static void CleanInvalidMetaFiles(string dirPath)
    {
        if (!Directory.Exists(dirPath)) return;
        DirectoryInfo dir = new DirectoryInfo(dirPath);
        foreach (FileInfo file in dir.GetFiles("*.meta", SearchOption.AllDirectories))
        {
            if (file.Length == 0)
            {
                try { file.Delete(); } catch { }
            }
        }
    }

    public static string GetMD5(string filePath)
    {
        using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] md5Info = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < md5Info.Length; i++)
                sb.Append(md5Info[i].ToString("x2"));

            return sb.ToString();
        }
    }
}
