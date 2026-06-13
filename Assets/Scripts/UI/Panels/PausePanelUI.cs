using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 战术暂停面板：继续 / 重启 / 返回基地。
/// </summary>
public class PausePanelUI : UiPanelBase
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainSceneName = "MainScene";

    /// <summary>绑定继续、重启与返回基地按钮。</summary>
    private void Awake()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResume);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestart);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }
    }

    /// <summary>显示时设置暂停标题文案。</summary>
    protected override void OnShow()
    {
        if (titleText != null)
        {
            titleText.text = "战术暂停";
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }
    }

    /// <summary>继续战斗按钮回调。</summary>
    private void OnResume()
    {
        PlayUiSfx("audio.sfx.ui_confirm");
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.ResumeGame();
        }
    }

    /// <summary>重新开始按钮回调。</summary>
    private void OnRestart()
    {
        PlayUiSfx("audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.RestartGame();
        }
    }

    /// <summary>返回基地按钮回调，清理 Run 并加载主场景。</summary>
    private void OnMainMenu()
    {
        PlayUiSfx("audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out SaveManager save))
        {
            save.ClearActiveRun();
        }

        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.OpenMainMenu();
        }

        if (!string.IsNullOrWhiteSpace(mainSceneName))
        {
            StartCoroutine(LoadMainSceneAfterBootstrapShutdown());
        }
    }

    /// <summary>销毁 Bootstrapper 后异步加载主场景。</summary>
    private IEnumerator LoadMainSceneAfterBootstrapShutdown()
    {
        if (GameBootstrapper.HasInstance)
        {
            Destroy(GameBootstrapper.Instance.gameObject);
            yield return null;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}
