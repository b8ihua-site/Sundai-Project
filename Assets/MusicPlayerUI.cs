using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicPlayerUI : MonoBehaviour
{
    public static MusicPlayerUI Instance;

    [Header("常時表示")]
    public GameObject playerButton;     // 左下の♪ボタン
    public TextMeshProUGUI nowPlayingText; // 右上の曲名

    [Header("パネル")]
    public GameObject musicPlayerPanel;
    public TextMeshProUGUI trackNameText; // パネル内の「曲名」表示

    [Header("コントロール")]
    public Button playPauseButton;
    public TextMeshProUGUI playPauseLabel;
    public Button prevButton;
    public Button nextButton;
    public Button shuffleButton;
    public TextMeshProUGUI shuffleLabel;
    public Button repeatButton;
    public TextMeshProUGUI repeatLabel;

    [Header("プレイリスト")]
    public Transform playlistRoot;
    public GameObject playlistItemPrefab;
    public int maxVisiblePlaylistItems = 6; // 背景の高さに収まる表示件数

    [Header("アイコン")]
    public Image playPauseIcon;
    public Sprite playSprite;
    public Sprite pauseSprite;
    [Range(0f, 1f)] public float iconOpacity = 0.8f;    // 通常時の不透明度
    [Range(0f, 1f)] public float iconOffOpacity = 0.1f; // シャッフル/リピートOFF時の不透明度

    [Header("レコード")]
    public RectTransform recordIcon;
    public float recordSpinSpeed = 90f; // 度/秒

    private bool panelOpen = false;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        musicPlayerPanel.SetActive(false);
        UpdateUI();

        playPauseButton.onClick.AddListener(OnPlayPause);
        prevButton.onClick.AddListener(() => MusicPlayer.Instance.Prev());
        nextButton.onClick.AddListener(() => MusicPlayer.Instance.Next());
        shuffleButton.onClick.AddListener(() => MusicPlayer.Instance.ToggleShuffle());
        repeatButton.onClick.AddListener(() => MusicPlayer.Instance.ToggleRepeat());
    }
void Update()
{
    if (Input.GetKeyDown(KeyCode.M))
        TogglePanel();

    if (Input.GetKeyDown(KeyCode.N))
        MusicPlayer.Instance.Next();

    if (Input.GetKeyDown(KeyCode.B))
        MusicPlayer.Instance.Prev();

    if (Input.GetKeyDown(KeyCode.P))
        OnPlayPause();

    if (Input.GetKeyDown(KeyCode.U))
        MusicPlayer.Instance.ToggleShuffle();

    if (Input.GetKeyDown(KeyCode.R))
        MusicPlayer.Instance.ToggleRepeat();

    if (recordIcon != null && MusicPlayer.Instance != null && MusicPlayer.Instance.IsPlaying)
        recordIcon.Rotate(0f, 0f, -recordSpinSpeed * Time.deltaTime);
}
    public void TogglePanel()
    {
        panelOpen = !panelOpen;
        musicPlayerPanel.SetActive(panelOpen);
    }

    void OnPlayPause()
    {
        if (MusicPlayer.Instance.IsPlaying)
            MusicPlayer.Instance.Pause();
        else
            MusicPlayer.Instance.Play();
    }

    void BuildPlaylist()
    {
        foreach (Transform child in playlistRoot)
            Destroy(child.gameObject);

        var tracks = MusicPlayer.Instance.tracks;
        var upcoming = MusicPlayer.Instance.GetUpcomingTrackOrder(maxVisiblePlaylistItems);
        foreach (int trackIndex in upcoming)
        {
            GameObject item = Instantiate(playlistItemPrefab, playlistRoot);
            var label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = tracks[trackIndex].name;

            var btn = item.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => MusicPlayer.Instance.PlayTrack(trackIndex));
        }
    }

    public void UpdateUI()
    {
        var clip = MusicPlayer.Instance.GetCurrentClip();
        string trackName = clip != null ? clip.name : "---";

        if (nowPlayingText != null)
            nowPlayingText.text = "♪ " + trackName;

        if (trackNameText != null)
            trackNameText.text = trackName;

        if (playPauseLabel != null)
            playPauseLabel.text = MusicPlayer.Instance.IsPlaying ? "II" : "▶";

        if (shuffleLabel != null)
            shuffleLabel.text = MusicPlayer.Instance.shuffle ? "SHUF ON" : "SHUF";

        if (repeatLabel != null)
            repeatLabel.text = MusicPlayer.Instance.repeat ? "REP ON" : "REP";

        if (playPauseIcon != null && playSprite != null && pauseSprite != null)
            playPauseIcon.sprite = MusicPlayer.Instance.IsPlaying ? pauseSprite : playSprite;

        SetIconAlpha(prevButton, iconOpacity);
        SetIconAlpha(nextButton, iconOpacity);
        SetIconAlpha(playPauseButton, iconOpacity);
        SetIconAlpha(shuffleButton, MusicPlayer.Instance.shuffle ? iconOpacity : iconOffOpacity);
        SetIconAlpha(repeatButton, MusicPlayer.Instance.repeat ? iconOpacity : iconOffOpacity);

        BuildPlaylist();
    }

    void SetIconAlpha(Button button, float alpha)
    {
        if (button == null) return;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, alpha);
        colors.highlightedColor = new Color(1f, 1f, 1f, Mathf.Min(1f, alpha + 0.15f));
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, alpha);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }
}