using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ABTools : EditorWindow
{
    private int nowSelIndex = 0;
    private string[] targetStrings = new string[] { "PC", "IOS", "Android" };
    //资源服务器默认IP地址
    private string serverIP = "ftp://192.168.5.35";

    [MenuItem("AB包工具/打开工具窗口")]
    private static void OpenWindow()
    {
        //获取一个ABTools 编辑器窗口对象
        ABTools windown = EditorWindow.GetWindowWithRect(typeof(ABTools), new Rect(0, 0, 350, 220)) as ABTools;
        windown.Show();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 150, 15), "平台选择");
        //页签显示 是从数组中取出字符串内容来显示 所以 需要改变当前选中的索引
        nowSelIndex = GUI.Toolbar(new Rect(10, 30, 250, 20), nowSelIndex, targetStrings);
        //资源服务器IP地址设置
        GUI.Label(new Rect(10, 60, 150, 15), "资源服务器地址");
        serverIP = GUI.TextField(new Rect(10, 80, 150, 20), serverIP);
        //创建对比文件 按钮
        if(GUI.Button(new Rect(10, 110, 100, 40), "创建对比文件"))
            CreateABCompareFile();
        //保存默认资源到StreamingAssets 按钮
        if (GUI.Button(new Rect(115, 110, 225, 40), "保存默认资源到StreamingAssets"))
            MoveABToStreamingAssets();
        //上传AB包和对比文件 按钮
        if (GUI.Button(new Rect(10, 160, 330, 40), "上传AB包和对比文件"))
            UploadAllABFile();
    }

    //生成AB包对比文件
    private void CreateABCompareFile()
    {
        //获取文件夹信息
        //要根据选择的平台读取对应平台文件夹下的内容 来进行对比文件的生成
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/ArtRes/AB/" + targetStrings[nowSelIndex]);
        //获取该目录下的所有文件信息
        FileInfo[] fileInfos = directory.GetFiles();

        //用于存储信息的 字符串
        string abCompareInfo = "";

        foreach (FileInfo info in fileInfos)
        {
            //没有后缀的 才是AB包 我们只想要AB包的信息
            if (info.Extension == "")
            {
                //Debug.Log("文件名：" + info.Name);
                //拼接一个AB包的信息
                abCompareInfo += info.Name + " " + info.Length + " " + GetMD5(info.FullName);
                //用一个分隔符分开不同文件之间的信息
                abCompareInfo += '|';
            }
        }
        if (abCompareInfo.Length > 0)
        {
            //因为循环完毕后 会在最后由一个 | 符号 所以 把它去掉
            abCompareInfo = abCompareInfo.Substring(0, abCompareInfo.Length - 1);
        }

        //存储拼接好的 AB包资源信息
        File.WriteAllText(Application.dataPath + "/ArtRes/AB/" + targetStrings[nowSelIndex] + "/ABCompareInfo.txt", abCompareInfo);
        //刷新编辑器
        AssetDatabase.Refresh();

        Debug.Log("AB包对比文件生成成功");
    }
    //获取文件MD5码
    private string GetMD5(string filePath)
    {
        //将文件以流的形式打开
        using (FileStream file = new FileStream(filePath, FileMode.Open))
        {
            //声明一个MD5对象 用于生成MD5码
            MD5 md5 = new MD5CryptoServiceProvider();
            //利用API 得到数据的MD5码 16个字节 数组
            byte[] md5Info = md5.ComputeHash(file);

            //把16个字节转换为 16进制 拼接成字符串 为了减小md5码的长度
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < md5Info.Length; i++)
                sb.Append(md5Info[i].ToString("x2"));

            return sb.ToString();
        }
    }

    //将选中资源移动到StreamingAssets文件夹中
    private void MoveABToStreamingAssets()
    {
        //通过编辑器Selection类中的方法 获取再Project窗口中选中的资源 
        UnityEngine.Object[] selectedAsset = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        //如果一个资源都没有选择 就没有必要处理后面的逻辑了
        if (selectedAsset.Length == 0)
            return;
        //用于拼接本地默认AB包资源信息的字符串
        string abCompareInfo = "";
        //遍历选中的资源对象
        foreach (UnityEngine.Object asset in selectedAsset)
        {
            //通过Assetdatabase类 获取 资源的路径
            string assetPath = AssetDatabase.GetAssetPath(asset);
            //截取路径当中的文件名 用于作为 StreamingAssets中的文件名
            string fileName = assetPath.Substring(assetPath.LastIndexOf('/'));

            //判断是否有.符号 如果有 证明有后缀 不处理
            if (fileName.IndexOf('.') != -1)
                continue;
            //你还可以在拷贝之前 去获取全路径 然后通过FIleInfo去获取后缀来判断 这样更加的准确

            //利用AssetDatabase中的API 将选中文件 复制到目标路径
            AssetDatabase.CopyAsset(assetPath, "Assets/StreamingAssets" + fileName);

            //获取拷贝到StreamingAssets文件夹中的文件的全部信息
            FileInfo fileInfo = new FileInfo(Application.streamingAssetsPath + fileName);
            //拼接AB包信息到字符串中
            // 注意：原代码中使用 CreateABCompare.GetMD5，这里保持原样不改动（若需要可替换为本文件的 GetMD5）
            abCompareInfo += fileInfo.Name + " " + fileInfo.Length + " " + CreateABCompare.GetMD5(fileInfo.FullName);
            //用一个符号隔开多个AB包信息
            abCompareInfo += "|";
        }
        if (abCompareInfo.Length > 0)
            //去掉最后一个|符号 为了之后拆分字符串方便
            abCompareInfo = abCompareInfo.Substring(0, abCompareInfo.Length - 1);
        //将本地默认资源的对比信息 存入文件
        File.WriteAllText(Application.streamingAssetsPath + "/ABCompareInfo.txt", abCompareInfo);
        //刷新窗口
        AssetDatabase.Refresh();
    }

    //上传AB包文件到服务器
    private void UploadAllABFile()
    {
        //获取文件夹信息
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/ArtRes/AB/" + targetStrings[nowSelIndex] + "/");
        //获取该目录下的所有文件信息
        FileInfo[] fileInfos = directory.GetFiles();

        foreach (FileInfo info in fileInfos)
        {
            //没有后缀的 才是AB包 我们只想要AB包的信息
            //还有需要获取 资源对比文件 格式是txt（该文件夹中 只有对比文件的格式才是txt 所以可以这样判断）
            if (info.Extension == "" ||
                info.Extension == ".txt")
            {
                //上传该文件
                FtpUploadFile(info.FullName, info.Name);
            }
        }
    }

    // 确保远程 FTP 目录存在（逐级创建），忽略已存在的目录错误（例如 550）
    private void EnsureFtpDirectoryExists(string directoryUri, NetworkCredential credential)
    {
        try
        {
            if (!directoryUri.EndsWith("/"))
                directoryUri += "/";

            Uri uri = new Uri(directoryUri);
            // 分割绝对路径的段（去掉空段）
            string[] segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // 构建根地址（含端口）
            string root = uri.Scheme + "://" + uri.Host;
            if (!uri.IsDefaultPort)
                root += ":" + uri.Port;
            // 逐级创建目录
            string current = root + "/";
            foreach (var seg in segments)
            {
                current = current + seg + "/";
                try
                {
                    FtpWebRequest req = (FtpWebRequest)WebRequest.Create(new Uri(current));
                    req.Credentials = credential;
                    req.Method = WebRequestMethods.Ftp.MakeDirectory;
                    req.Proxy = null;
                    req.KeepAlive = false;
                    using (var resp = (FtpWebResponse)req.GetResponse())
                    {
                        // 创建成功，可以记录（可选）
                    }
                }
                catch (WebException wex)
                {
                    // 如果目录已存在，部分 FTP 服务器返回 550 或特定说明，忽略该错误
                    var ftpResp = wex.Response as FtpWebResponse;
                    if (ftpResp != null)
                    {
                        // 550 通常表示目录已存在或不可用，忽略以继续创建下一级
                        if (ftpResp.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable ||
                            ftpResp.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailableOrBusy)
                        {
                            // 忽略
                        }
                        else
                        {
                            Debug.LogWarning($"EnsureFtpDirectoryExists: 目录创建异常 {current} -> {ftpResp.StatusDescription}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"EnsureFtpDirectoryExists: 无响应或其他错误 {current} -> {wex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"EnsureFtpDirectoryExists: 异常 {current} -> {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("EnsureFtpDirectoryExists 总体异常: " + ex.Message);
        }
    }

    //异步上传文件（现在会在上传前确保远程目录存在）
    private async void FtpUploadFile(string filePath, string fileName)
    {
        await Task.Run(() =>
        {
            try
            {
                // 构造远程目录 URI（不含文件名）
                string remoteDir = serverIP.TrimEnd('/') + "/AB/" + targetStrings[nowSelIndex] + "/";
                // 准备凭证
                NetworkCredential n = new NetworkCredential("user1", "123");
                // 在上传前确保远程目录存在（逐级创建）
                EnsureFtpDirectoryExists(remoteDir, n);

                //1.创建一个FTP连接 用于上传文件（包含文件名）
                Uri fileUri = new Uri(remoteDir + fileName);
                FtpWebRequest req = FtpWebRequest.Create(fileUri) as FtpWebRequest;
                //2.设置一个通信凭证 这样才能上传
                req.Credentials = n;
                //3.其它设置
                //  设置代理为null
                req.Proxy = null;
                //  请求完毕后 是否关闭控制连接
                req.KeepAlive = false;
                //  操作命令-上传
                req.Method = WebRequestMethods.Ftp.UploadFile;
                //  指定传输的类型 2进制
                req.UseBinary = true;

                // 设置 FileShare.ReadWrite，防止文件共享锁定
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    req.ContentLength = file.Length;

                    //4.上传文件
                    using (Stream upLoadStream = req.GetRequestStream())
                    {
                        //一点一点的上传内容
                        byte[] bytes = new byte[2048];
                        int contentLength = file.Read(bytes, 0, bytes.Length);

                        //循环上传文件中的数据
                        while (contentLength > 0)
                        {
                            //写入到上传流中
                            upLoadStream.Write(bytes, 0, contentLength);
                            //写完再读
                            contentLength = file.Read(bytes, 0, bytes.Length);
                        }
                    }

                    // 获取服务器响应以确认上传结果
                    try
                    {
                        using (var resp = (FtpWebResponse)req.GetResponse())
                        {
                            Debug.Log($"{fileName} 上传成功，服务器响应：{resp.StatusDescription}");
                        }
                    }
                    catch (WebException wex)
                    {
                        var ftpResp = wex.Response as FtpWebResponse;
                        if (ftpResp != null)
                            Debug.LogError($"{fileName} 上传失败：{ftpResp.StatusDescription}");
                        else
                            Debug.LogError($"{fileName} 上传失败：{wex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("上传异常: " + ex.Message);
            }
        });

    }
}
