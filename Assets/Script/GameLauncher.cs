using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameLauncher : MonoBehaviour
{
    [Header("UI 控件绑定（可选）")]
    public Text statusText;          // 显示更新状态文本
    public Button startButton;        // 开始游戏按钮
    public Slider progressBar;        // 下载进度条

    [Header("加载设置")]
    public string targetSceneName = "Game"; // 点击开始游戏后加载的目标场景

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false); // 更新完成前隐藏开始按钮
            startButton.onClick.AddListener(OnStartGameClicked);
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.value = 0;
        }
    }

    private async void Start()
    {
        UpdateStatus("正在检查网络与资源版本...", 0f);

        bool ok = await ABUpdateMgr.Instance.CheckUpdate((msg) =>
        {
            Debug.Log("[GameLauncher] " + msg);
            UpdateStatus(msg, ParseProgress(msg));
        });

        if (ok)
        {
            UpdateStatus("资源更新完成！点击按钮开始游戏", 1f);
        }
        else
        {
            UpdateStatus("网络连接失败，已切换至离线模式。点击按钮开始游戏", 1f);
        }

        // 显示开始游戏按钮
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
        }
        else
        {
            // 如果未绑定按钮，1.5 秒后自动进入游戏
            Invoke(nameof(OnStartGameClicked), 1.5f);
        }
    }

    private void UpdateStatus(string msg, float progress)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }

        if (progressBar != null)
        {
            if (progress > 0)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = progress;
            }
        }
    }

    private float ParseProgress(string msg)
    {
        // 尝试解析形如 "下载进度: 2/5" 的进度比例
        if (msg.Contains("下载进度:") && msg.Contains("/"))
        {
            try
            {
                string parts = msg.Split(':')[1].Trim();
                string[] nums = parts.Split('/');
                float current = float.Parse(nums[0]);
                float total = float.Parse(nums[1]);
                return Mathf.Clamp01(current / total);
            }
            catch { }
        }
        return 0f;
    }

    public void OnStartGameClicked()
    {
        Debug.Log($"[GameLauncher] 加载目标游戏场景: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
}
