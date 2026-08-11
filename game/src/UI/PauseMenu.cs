using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 暂停菜单
/// Esc 键打开：继续/存档/读档/设置/返回主菜单/退出
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject menuPanel;

    [Header("按钮")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("设置子面板")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private bool _isOpen;

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (continueButton != null) continueButton.onClick.AddListener(Resume);
        if (saveButton != null) saveButton.onClick.AddListener(OnSave);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoad);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.ConsumeMenu())
        {
            if (_isOpen)
                Resume();
            else
                Open();
        }
    }

    public void Open()
    {
        _isOpen = true;
        if (menuPanel != null) menuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        GameManager.Instance.PauseGame();
        InputManager.Instance.IsInputLocked = true;
    }

    public void Resume()
    {
        _isOpen = false;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        GameManager.Instance.ResumeGame();
        InputManager.Instance.IsInputLocked = false;
    }

    private void OnSave()
    {
        SaveManager.Instance?.SaveToSlot(0);
    }

    private void OnLoad()
    {
        SaveManager.Instance?.LoadFromSlot(0);
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnBGMVolumeChanged(float volume)
    {
        AudioManager.Instance?.SetBGMVolume(volume);
    }

    private void OnSFXVolumeChanged(float volume)
    {
        AudioManager.Instance?.SetSFXVolume(volume);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
