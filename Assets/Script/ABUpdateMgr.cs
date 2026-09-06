using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ABUpdateMgr : MonoBehaviour
{
    private static ABUpdateMgr instance;

    public static ABUpdateMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ABUpdateMgr>();
                if (instance == null)
                {
                    GameObject newObj = new GameObject("ABUpdateMgr");
                    instance = newObj.AddComponent<ABUpdateMgr>();
                }
            }
            return instance;
        }
    }

    public string serverIP = "ftp://192.168.5.35";
    public string ftpUser = "user1";
    public string ftpPassword = "123";
    public int timeoutMs = 5000; // 网络请求超时时间（毫秒），提升为 5 秒容错
    public bool enableUpdate = true; // 是否开启热更新检测

    private static bool hasCheckedOnStartup = false; // 标记是否在游戏启动时已检测过

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        hasCheckedOnStartup = false;
    }

    private Dictionary<string, ABInfo> remoteABInfo = new Dictionary<string, ABInfo>();
    private Dictionary<string, ABInfo> localABInfo = new Dictionary<string, ABInfo>();
    private List<string> downLoadList = new List<string>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    /// <summary>
    /// 检测并执行热更新（全异步过程）
    /// </summary>
    /// <param name="updateInfoCallBack">进度与状态回调（主线程安全）</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> CheckUpdate(UnityAction<string> updateInfoCallBack = null)
    {
        // 如果本次启动已经检测过更新，重新加载场景时直接跳过
        if (hasCheckedOnStartup)
        {
            updateInfoCallBack?.Invoke("跳过重复热更新检测");
            return true;
        }
        hasCheckedOnStartup = true;

        if (!enableUpdate)
        {
            updateInfoCallBack?.Invoke("热更新功能已跳过（开发测试模式）");
            return true;
        }

        remoteABInfo.Clear();
        localABInfo.Clear();
        downLoadList.Clear();

        // 1. 下载远程对比文件
        updateInfoCallBack?.Invoke("开始下载对比资源...");
        bool downloadCompareOk = await DownLoadABCompareFileAsync();
        if (!downloadCompareOk)
        {
            updateInfoCallBack?.Invoke("连接远程更新服务器失败，使用本地资源运行");
            Debug.LogWarning($"[ABUpdateMgr] 无法连接到FTP服务器 ({serverIP})，已自动跳过热更新。");
            return false;
        }

        // 2. 异步读取远程对比文件
        updateInfoCallBack?.Invoke("解析对比文件...");
        string remoteInfo = null;
        try
        {
            remoteInfo = await File.ReadAllTextAsync(Application.persistentDataPath + "/ABCompareInfo_TMP.txt");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ABUpdateMgr] 读取临时对比文件失败: " + ex.Message);
            return false;
        }

        // 3. 解析远程信息
        updateInfoCallBack?.Invoke("解析远程对比文件信息...");
        GetRemoteABCompareFileInfo(remoteInfo, remoteABInfo);
        updateInfoCallBack?.Invoke("解析远程对比文件成功");

        // 4. 异步加载本地对比信息
        bool localOk = await GetLocalABCompareFileInfoAsync();
        if (!localOk)
            return false;

        // 5. 对比远程与本地，生成下载列表
        updateInfoCallBack?.Invoke("开始比对文件差异...");
        foreach (string abName in remoteABInfo.Keys)
        {
            if (!localABInfo.ContainsKey(abName))
            {
                downLoadList.Add(abName);
            }
            else
            {
                if (localABInfo[abName].md5 != remoteABInfo[abName].md5)
                    downLoadList.Add(abName);
                localABInfo.Remove(abName);
            }
        }
        updateInfoCallBack?.Invoke("比对完成");

        // 6. 删除本地多余的旧AB包
        updateInfoCallBack?.Invoke("清理旧版本废弃资源...");
        foreach (string abName in localABInfo.Keys)
        {
            string delPath = Application.persistentDataPath + "/" + abName;
            if (File.Exists(delPath))
                File.Delete(delPath);
        }

        // 7. 下载需要更新的AB包
        if (downLoadList.Count > 0)
        {
            updateInfoCallBack?.Invoke($"开始下载资源包(共{downLoadList.Count}个)...");
            bool downloadOk = await DownLoadABFileAsync(updateInfoCallBack);
            if (!downloadOk)
            {
                updateInfoCallBack?.Invoke("部分资源包下载失败");
                return false;
            }
        }

        // 8. 异步写入本地对比文件
        updateInfoCallBack?.Invoke("更新本地版本对比记录...");
        await File.WriteAllTextAsync(Application.persistentDataPath + "/ABCompareInfo.txt", remoteInfo);

        updateInfoCallBack?.Invoke("热更新完成！");
        return true;
    }

    /// <summary>
    /// 异步下载远程对比文件
    /// </summary>
    private async Task<bool> DownLoadABCompareFileAsync()
    {
        bool isOver = false;
        int retryCount = 2;
        string localPath = Application.persistentDataPath;

        while (!isOver && retryCount > 0)
        {
            var (success, errorMsg) = await Task.Run(() =>
            {
                return DownLoadFile("ABCompareInfo.txt", localPath + "/ABCompareInfo_TMP.txt");
            });
            isOver = success;
            if (!isOver && !string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogWarning($"[ABUpdateMgr] FTP下载对比文件失败: {errorMsg}");
            }
            retryCount--;
        }
        return isOver;
    }

    /// <summary>
    /// 异步读取本地对比文件信息（包装协程）
    /// </summary>
    private async Task<bool> GetLocalABCompareFileInfoAsync()
    {
        string filePath = null;

        if (File.Exists(Application.persistentDataPath + "/ABCompareInfo.txt"))
        {
            filePath = "file:///" + Application.persistentDataPath + "/ABCompareInfo.txt";
        }
        else if (File.Exists(Application.streamingAssetsPath + "/ABCompareInfo.txt"))
        {
            filePath =
#if UNITY_ANDROID
                Application.streamingAssetsPath;
#else
                "file:///" + Application.streamingAssetsPath;
#endif
            filePath += "/ABCompareInfo.txt";
        }
        else
        {
            return true;
        }

        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(LoadLocalCompareFileCoroutine(filePath, tcs));
        return await tcs.Task;
    }

    private IEnumerator LoadLocalCompareFileCoroutine(string filePath, TaskCompletionSource<bool> tcs)
    {
        UnityWebRequest req = UnityWebRequest.Get(filePath);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            GetRemoteABCompareFileInfo(req.downloadHandler.text, localABInfo);
            tcs.SetResult(true);
        }
        else
        {
            tcs.SetResult(false);
        }
        req.Dispose();
    }

    /// <summary>
    /// 异步批量下载AB文件
    /// </summary>
    private async Task<bool> DownLoadABFileAsync(UnityAction<string> updatePro)
    {
        string localPath = Application.persistentDataPath + "/";
        int maxConcurrent = 3;
        int retryCount = 0;
        int maxRetry = 2;
        int downLoadOverNum = 0;
        int downLoadMaxNum = downLoadList.Count;

        while (downLoadList.Count > 0 && retryCount < maxRetry)
        {
            var batch = downLoadList.Take(maxConcurrent).ToList();
            var tasks = batch.Select(name => Task.Run(() =>
            {
                var (success, errorMsg) = DownLoadFile(name, localPath + name);
                return (name, success, errorMsg);
            })).ToArray();

            var results = await Task.WhenAll(tasks);

            foreach (var (name, success, errorMsg) in results)
            {
                if (success)
                {
                    downLoadOverNum++;
                    updatePro?.Invoke($"下载进度: {downLoadOverNum}/{downLoadMaxNum}");
                    downLoadList.Remove(name);
                }
                else
                {
                    Debug.LogWarning($"[ABUpdateMgr] 下载AB包 {name} 失败: {errorMsg}");
                }
            }

            retryCount++;
        }

        return downLoadList.Count == 0;
    }

    /// <summary>
    /// 下载单文件（返回成功状态与错误消息）
    /// </summary>
    private (bool success, string errorMsg) DownLoadFile(string fileName, string localPath)
    {
        try
        {
            string pInfo =
#if UNITY_IOS
            "IOS";
#elif UNITY_ANDROID
            "Android";
#else
            "PC";
#endif
            string requestUrl = serverIP.TrimEnd('/') + "/AB/" + pInfo + "/" + fileName;
            FtpWebRequest req = FtpWebRequest.Create(new Uri(requestUrl)) as FtpWebRequest;
            NetworkCredential n = new NetworkCredential(ftpUser, ftpPassword);
            req.Credentials = n;
            req.Proxy = null;
            req.KeepAlive = false;
            req.Method = WebRequestMethods.Ftp.DownloadFile;
            req.UseBinary = true;

            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;

            // 确保本地目标文件夹自动创建
            string dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (FtpWebResponse res = req.GetResponse() as FtpWebResponse)
            {
                using (Stream downLoadStream = res.GetResponseStream())
                {
                    using (FileStream file = File.Create(localPath))
                    {
                        byte[] bytes = new byte[2048];
                        int contentLength = downLoadStream.Read(bytes, 0, bytes.Length);
                        while (contentLength != 0)
                        {
                            file.Write(bytes, 0, contentLength);
                            contentLength = downLoadStream.Read(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 解析对比文件字符串
    /// </summary>
    public void GetRemoteABCompareFileInfo(string info, Dictionary<string, ABInfo> ABInfo)
    {
        if (string.IsNullOrEmpty(info)) return;

        string[] strs = info.Split('|');
        for (int i = 0; i < strs.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(strs[i])) continue;
            string[] infos = strs[i].Split(' ');
            if (infos.Length >= 3)
            {
                ABInfo[infos[0]] = new ABInfo(infos[0], infos[1], infos[2]);
            }
        }
    }

    private AssetBundle loadedLuaAB = null;

    /// <summary>
    /// 从已热更新的 lua AssetBundle 中获取最新的 TextAsset
    /// </summary>
    public TextAsset GetLuaTextAsset(string scriptName)
    {
        try
        {
            if (loadedLuaAB == null)
            {
                string persistentPath = Application.persistentDataPath + "/lua";
                string streamingPath = Application.streamingAssetsPath + "/lua";

                if (File.Exists(persistentPath))
                {
                    loadedLuaAB = AssetBundle.LoadFromFile(persistentPath);
                }
                else if (File.Exists(streamingPath))
                {
                    loadedLuaAB = AssetBundle.LoadFromFile(streamingPath);
                }
            }

            if (loadedLuaAB != null)
            {
                TextAsset asset = loadedLuaAB.LoadAsset<TextAsset>(scriptName);
                if (asset == null)
                    asset = loadedLuaAB.LoadAsset<TextAsset>(scriptName + ".lua");
                if (asset == null)
                    asset = loadedLuaAB.LoadAsset<TextAsset>(scriptName + ".txt");
                if (asset == null)
                    asset = loadedLuaAB.LoadAsset<TextAsset>("Assets/ArtRes/Lua/" + scriptName);

                return asset;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ABUpdateMgr] 从热更包读取脚本异常: " + ex.Message);
        }
        return null;
    }

    private void OnDestroy()
    {
        if (loadedLuaAB != null)
        {
            loadedLuaAB.Unload(true);
            loadedLuaAB = null;
        }
        if (instance == this)
        {
            instance = null;
        }
    }

    public class ABInfo
    {
        public string name;
        public long size;
        public string md5;

        public ABInfo(string name, string size, string md5)
        {
            this.name = name;
            this.size = long.Parse(size);
            this.md5 = md5;
        }
    }
}