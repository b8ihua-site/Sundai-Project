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

    private bool panelOpen = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        musicPlayerPanel.SetActive(false);
        BuildPlaylist();
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
        for (int i = 0; i < tracks.Length; i++)
        {
            int index = i;
            GameObject item = Instantiate(playlistItemPrefab, playlistRoot);
            var label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = tracks[i].name;

            var btn = item.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => MusicPlayer.Instance.PlayTrack(index));
        }
    }

    public void UpdateUI()
    {
        var clip = MusicPlayer.Instance.GetCurrentClip();
        string trackName = clip != null ? clip.name : "---";

        if (nowPlayingText != null)
            nowPlayingText.text = "♪ " + trackName;

        if (playPauseLabel != null)
            playPauseLabel.text = MusicPlayer.Instance.IsPlaying ? "II" : "▶";

        if (shuffleLabel != null)
            shuffleLabel.text = MusicPlayer.Instance.shuffle ? "SHUF ON" : "SHUF";

        if (repeatLabel != null)
            repeatLabel.text = MusicPlayer.Instance.repeat ? "REP ON" : "REP";
    }
}