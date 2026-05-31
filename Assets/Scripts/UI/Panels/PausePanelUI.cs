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

    protected override void OnShow()
    {
        if (titleText != null)
        {
            titleText.text = "战术暂停";
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }
    }

    private void OnResume()
    {
        PlayUiSfx("audio.sfx.ui_confirm");
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.ResumeGame();
        }
    }

    private void OnRestart()
    {
        PlayUiSfx("audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.RestartGame();
        }
    }

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
