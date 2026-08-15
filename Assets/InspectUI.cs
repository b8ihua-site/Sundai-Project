using UnityEngine;
using TMPro;
using System.Collections;

public class InspectUI : MonoBehaviour
{
    [Header("ヒントUI")]
    public GameObject hintRoot;
    public TextMeshProUGUI hintLabel;

    [Header("文章UI")]
    public GameObject inspectRoot;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI pageIndicator;
    public TextMeshProUGUI pressPrompt;

    [Header("NPC会話UI")]
    public GameObject speakerNameRoot;   // 話者名の背景パネルなど（任意）
    public TextMeshProUGUI speakerNameText; // 話者名のTMPro

    [Header("文字送り設定")]
    public float charInterval = 0.05f;

    [Header("音声")]
    public AudioSource voiceAudioSource; // InspectUI側でも持つと管理しやすい

    private string[] currentPages;
    private int currentPageIndex;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    // NPC情報を一時保持
    private bool currentIsNPC = false;
    private string currentSpeakerName = "";
    private AudioClip[] currentVoiceClips;

    void Start()
    {
        hintRoot.SetActive(false);
        inspectRoot.SetActive(false);
        SetSpeakerVisible(false);
    }

    public void ShowHint(bool show)
    {
        hintRoot.SetActive(show);
    }

    public void ShowHintText(string text)
    {
        if (hintLabel != null)
            hintLabel.text = text;
        ShowHint(true);
    }

    // ★ NPC情報も受け取れるようにオーバーロード
    public void StartReading(string[] pages, bool isNPC = false, string speakerName = "", AudioClip[] voiceClips = null)
    {
        currentPages = pages;
        currentPageIndex = 0;
        currentIsNPC = isNPC;
        currentSpeakerName = speakerName;
        currentVoiceClips = voiceClips;

        // 話者名表示
        if (isNPC && speakerNameText != null)
        {
            speakerNameText.text = speakerName;
            SetSpeakerVisible(true);
        }
        else
        {
            SetSpeakerVisible(false);
        }

        inspectRoot.SetActive(true);
        DisplayCurrentPage();
    }

    public bool NextPage()
    {
        if (isTyping)
        {
            SkipTyping();
            return false;
        }

        currentPageIndex++;
        if (currentPageIndex >= currentPages.Length)
        {
            inspectRoot.SetActive(false);
            SetSpeakerVisible(false);
            StopVoice();
            return true;
        }
        DisplayCurrentPage();
        return false;
    }

    void DisplayCurrentPage()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (pressPrompt != null)
            pressPrompt.gameObject.SetActive(false);

        // ★ ページに対応する音声を再生
        PlayVoice(currentPageIndex);

        typingCoroutine = StartCoroutine(TypeText(currentPages[currentPageIndex]));

        if (pageIndicator != null)
        {
            pageIndicator.text = currentPages.Length > 1
                ? $"{currentPageIndex + 1} / {currentPages.Length}"
                : "";
        }
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        bodyText.text = "";

        foreach (char c in text)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(charInterval);
        }

        isTyping = false;

        if (pressPrompt != null)
            pressPrompt.gameObject.SetActive(true);
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        bodyText.text = currentPages[currentPageIndex];
        isTyping = false;

        if (pressPrompt != null)
            pressPrompt.gameObject.SetActive(true);
    }

    // ★ 音声再生ヘルパー
    void PlayVoice(int index)
    {
        if (voiceAudioSource == null) return;
        if (currentVoiceClips == null || currentVoiceClips.Length == 0) return;
        if (index >= currentVoiceClips.Length) return;
        if (currentVoiceClips[index] == null) return;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = currentVoiceClips[index];
        voiceAudioSource.Play();
    }

    void StopVoice()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
    }

    void SetSpeakerVisible(bool visible)
    {
        if (speakerNameRoot != null)
            speakerNameRoot.SetActive(visible);
        else if (speakerNameText != null)
            speakerNameText.gameObject.SetActive(visible);
    }
}