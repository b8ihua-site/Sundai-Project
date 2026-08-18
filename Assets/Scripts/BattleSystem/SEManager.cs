using UnityEngine;
using System.Collections.Generic;

public class SEManager : MonoBehaviour
{
    public static SEManager Instance;

    [System.Serializable]
    public class SE
    {
        public string key;          // 呼び出し名 例:"attack"
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("SE一覧")]
    public List<SE> seList = new List<SE>();

    [Header("音階用（ピッチ変更で鳴らす）")]
    public AudioClip scaleClip;                 // ドの音1個。短い単音(ベル/マリンバ等)推奨
    [Range(0f, 1f)] public float scaleVolume = 1f;

    [Header("設定")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    private AudioSource source;       // 通常SE用
    private AudioSource pitchSource;  // 音階用（ピッチを変える）
    private Dictionary<string, SE> dict = new Dictionary<string, SE>();

    // ド,レ,ミ,ファ,ソ,ラ,シ,ド の半音オフセット
    private static readonly int[] majorScale = { 0, 2, 4, 5, 7, 9, 11, 12 };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        pitchSource = gameObject.AddComponent<AudioSource>();
        pitchSource.playOnAwake = false;

        foreach (var se in seList)
            if (se != null && !string.IsNullOrEmpty(se.key) && !dict.ContainsKey(se.key))
                dict.Add(se.key, se);

        masterVolume = PlayerPrefs.GetFloat("SeVolume", 1f);
    }

    // メニューの設定画面から呼ばれる
    public void SetVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("SeVolume", masterVolume);
    }

    // 通常SE
    public void Play(string key)
    {
        if (dict.TryGetValue(key, out var se) && se.clip != null)
            source.PlayOneShot(se.clip, se.volume * masterVolume);
        else
            Debug.LogWarning($"SE '{key}' が見つかりません");
    }

    // n番目の音(0始まり)をドレミの音階で鳴らす。0=ド,1=レ,2=ミ…
    public void PlayScaleNote(int noteIndex)
    {
        if (scaleClip == null)
        {
            Debug.LogWarning("scaleClip が未設定です");
            return;
        }

        int octave = noteIndex / majorScale.Length;
        int degree = noteIndex % majorScale.Length;
        int semitone = majorScale[degree] + octave * 12;

        // 上げすぎ防止（最大2オクターブ）
        semitone = Mathf.Min(semitone, 24);

        pitchSource.pitch = Mathf.Pow(2f, semitone / 12f);
        pitchSource.PlayOneShot(scaleClip, scaleVolume * masterVolume);
    }
}