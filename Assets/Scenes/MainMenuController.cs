using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Main menu Fish Hunt: NEW GAME / CONTINUE / SETTINGS / EXIT.
/// Item terpilih tampil dengan kurung "[ NEW GAME ]" seperti di MainMenuScene.
/// Kontrol: W/S atau panah atas/bawah, Enter/Space untuk memilih, mouse hover + klik.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public enum MenuAction { NewGame, Continue, Settings, Exit }

    [Serializable]
    public class MenuItem
    {
        public MenuAction action;
        [Tooltip("Teks asli tanpa kurung, contoh: NEW GAME")]
        public string text = "NEW GAME";
        public TMP_Text label;
    }

    [Header("Menu Items (urutan dari atas ke bawah)")]
    [SerializeField] private MenuItem[] items = new MenuItem[]
    {
        new MenuItem { action = MenuAction.NewGame,  text = "NEW GAME" },
        new MenuItem { action = MenuAction.Continue, text = "CONTINUE" },
        new MenuItem { action = MenuAction.Settings, text = "SETTINGS" },
        new MenuItem { action = MenuAction.Exit,     text = "EXIT" },
    };

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Warna (mirip tampilan asli)")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(0.78f, 0.88f, 1f, 0.85f);
    [SerializeField] private Color disabledColor = new Color(0.78f, 0.88f, 1f, 0.3f);

    [Header("Settings Panel (opsional)")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button settingsBackButton;

    [Header("Transisi (opsional)")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Audio (opsional)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip moveSfx;
    [SerializeField] private AudioClip selectSfx;

    public const string SaveKey = "HasSave";
    private const string VolumeKey = "MasterVolume";

    private int index;
    private bool busy;

    private bool HasSave => PlayerPrefs.GetInt(SaveKey, 0) == 1;

    private void Awake()
    {
        // Pasang handler hover/klik otomatis ke tiap label.
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].label == null) continue;
            var hover = items[i].label.GetComponent<MenuItemHover>();
            if (hover == null) hover = items[i].label.gameObject.AddComponent<MenuItemHover>();
            hover.controller = this;
            hover.index = i;
        }

        float vol = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = vol;
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(vol);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (fadeGroup != null) fadeGroup.alpha = 0f;
    }

    private void Start()
    {
        index = FirstEnabledIndex();
        Refresh();
    }

    private void Update()
    {
        if (busy) return;

        if (SettingsOpen)
        {
            if (PressedBack()) CloseSettings();
            return;
        }

        if (PressedUp()) Move(-1);
        else if (PressedDown()) Move(1);
        else if (PressedConfirm()) Activate();
    }

    // ---------- Navigasi ----------

    private bool IsEnabled(int i)
    {
        return items[i].action != MenuAction.Continue || HasSave;
    }

    private int FirstEnabledIndex()
    {
        for (int i = 0; i < items.Length; i++) if (IsEnabled(i)) return i;
        return 0;
    }

    private void Move(int dir)
    {
        int next = index;
        for (int n = 0; n < items.Length; n++)
        {
            next = (next + dir + items.Length) % items.Length;
            if (IsEnabled(next)) break;
        }
        Select(next);
    }

    /// <summary>Dipanggil oleh keyboard atau hover mouse.</summary>
    public void Select(int i)
    {
        if (busy || SettingsOpen) return;
        if (i < 0 || i >= items.Length || !IsEnabled(i) || i == index) return;
        index = i;
        Play(moveSfx);
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            if (it.label == null) continue;

            if (!IsEnabled(i))
            {
                it.label.text = it.text;
                it.label.color = disabledColor;
            }
            else if (i == index)
            {
                it.label.text = "[ " + it.text + " ]";
                it.label.color = selectedColor;
            }
            else
            {
                it.label.text = it.text;
                it.label.color = normalColor;
            }
        }
    }

    // ---------- Aksi ----------

    /// <summary>Dipanggil oleh Enter/Space atau klik mouse.</summary>
    public void Activate()
    {
        if (busy || SettingsOpen) return;
        if (!IsEnabled(index)) return;

        Play(selectSfx);

        switch (items[index].action)
        {
            case MenuAction.NewGame: NewGame(); break;
            case MenuAction.Continue: Continue(); break;
            case MenuAction.Settings: OpenSettings(); break;
            case MenuAction.Exit: ExitGame(); break;
        }
    }

    // Public supaya bisa juga dipasang manual ke OnClick Button kalau mau.
    public void NewGame()
    {
        PlayerPrefs.DeleteKey(SaveKey); // reset save lama; hapus baris ini kalau save dikelola script lain
        PlayerPrefs.Save();
        StartCoroutine(LoadSceneRoutine(gameSceneName));
    }

    public void Continue()
    {
        if (!HasSave) return;
        StartCoroutine(LoadSceneRoutine(gameSceneName));
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        PlayerPrefs.Save();
    }

    public void ExitGame()
    {
        busy = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool SettingsOpen => settingsPanel != null && settingsPanel.activeSelf;

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        busy = true;

        if (fadeGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneName);
    }

    private void Play(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }

    // ---------- Input (kompatibel Input System baru & lama) ----------

    private bool PressedUp()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
    }

    private bool PressedDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
    }

    private bool PressedConfirm()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private bool PressedBack()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
