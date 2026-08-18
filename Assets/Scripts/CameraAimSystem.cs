using UnityEngine;
using Unity.Cinemachine;

public class CameraAimSystem : MonoBehaviour
{
    [Header("参照")]
    public GameObject cmObject;   // CameraZoomと同じCinemachine 3rd Person Follow
    public GameObject crosshair;  // 構え中に表示する照準UI（別Canvas）
    public GameObject uiRoot;     // 構え中に非表示にするUI全体（メインCanvas）

    [Header("構え設定")]
    public float aimDistance = 1.5f;      // 構え時のカメラ距離
    public float aimTransitionSpeed = 8f; // ズームの補間速度
    public float foundZoomDistance = 0.9f; // 発見した瞬間にさらに寄るカメラ距離

    [Header("撮影設定")]
    public float photoRange = 30f;
    public LayerMask photoLayer = ~0;

    [Header("発見メッセージ")]
    public string cancelLabel = "みのがす";
    public string foundMessageFormat = "{0}を みつけた！";

    [Header("効果音")]
    public AudioSource audioSource;
    public AudioClip cameraSetSE;  // 構え始めた時
    public AudioClip cameraShotSE; // 撮影した時
    public AudioClip encounterSE;  // 何かを見つけた時

    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private StarterAssets.ThirdPersonController controller;
    private Renderer[] avatarRenderers;
    private float normalDistance;
    private bool isAiming = false;
    private bool awaitingChoice = false; // 発見演出～たたかう/みのがす選択待ちの間
    private IPhotographable currentAimTarget;

    void Start()
    {
        if (cmObject != null)
            thirdPersonFollow = cmObject.GetComponent<Cinemachine3rdPersonFollow>();

        controller = GetComponentInParent<StarterAssets.ThirdPersonController>();
        avatarRenderers = GetComponentsInChildren<Renderer>(true);

        if (crosshair != null)
            crosshair.SetActive(false);

        // 戦闘から戻った直後などにuiRootが非表示のまま残らないよう、シーン開始時は必ず表示状態にする
        if (uiRoot != null)
            uiRoot.SetActive(true);
    }

    void Update()
    {
        if (awaitingChoice) return; // 選択待ち中は構え/撮影の入力を受け付けない

        if (Input.GetMouseButtonDown(1))
            StartAim();

        if (Input.GetMouseButtonUp(1))
            EndAim();

        if (isAiming && thirdPersonFollow != null)
        {
            thirdPersonFollow.CameraDistance = Mathf.Lerp(
                thirdPersonFollow.CameraDistance, aimDistance, Time.deltaTime * aimTransitionSpeed);
        }

        if (isAiming)
            UpdateAimTarget();
    }

    // 構え中、今まさに画面中央に捉えている対象を毎フレーム調べ、変化があればハイライトを切り替える
    void UpdateAimTarget()
    {
        IPhotographable found = FindPhotoTarget();

        if (found == currentAimTarget) return;

        currentAimTarget?.SetAimHighlight(false);
        found?.SetAimHighlight(true);
        currentAimTarget = found;
    }

    IPhotographable FindPhotoTarget()
    {
        var cam = Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        var hits = Physics.RaycastAll(ray, photoRange, photoLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // 自分自身（見えなくなっているだけで当たり判定は残っているアバター）は無視する
            if (hit.collider.transform.IsChildOf(transform.root))
                continue;

            return hit.collider.GetComponent<IPhotographable>(); // 風以外なら null（＝手前に遮る物がある）
        }

        return null;
    }

    void StartAim()
    {
        isAiming = true;

        if (thirdPersonFollow != null)
            normalDistance = thirdPersonFollow.CameraDistance;

        if (controller != null)
            controller.movementLocked = true;

        if (audioSource != null && cameraSetSE != null)
            audioSource.PlayOneShot(cameraSetSE);

        SetAvatarVisible(false);

        if (uiRoot != null)
            uiRoot.SetActive(false);

        if (crosshair != null)
            crosshair.SetActive(true);
    }

    void EndAim()
    {
        if (!isAiming) return;
        isAiming = false;

        if (audioSource != null && cameraShotSE != null)
            audioSource.PlayOneShot(cameraShotSE);

        var target = FindPhotoTarget();

        if (target != null)
            HandleFound(target);
        else
            RestoreFromAim();
    }

    void HandleFound(IPhotographable target)
    {
        awaitingChoice = true;

        target.OnPhotographed();

        // カメラをさらに寄せて「アップ」にする
        if (thirdPersonFollow != null)
            thirdPersonFollow.CameraDistance = foundZoomDistance;

        if (audioSource != null && encounterSE != null)
            audioSource.PlayOneShot(encounterSE);

        // BattleChoiceUIのパネルはuiRoot（メインCanvas）の子なので、
        // 表示前にuiRootを戻しておかないと親ごと非表示のまま操作不能になる
        if (uiRoot != null)
            uiRoot.SetActive(true);

        if (crosshair != null)
            crosshair.SetActive(false);

        string message = string.Format(foundMessageFormat, target.DisplayName);

        if (BattleChoiceUI.Instance != null)
        {
            BattleChoiceUI.Instance.Show(message, cancelLabel,
                onFightCallback: () =>
                {
                    awaitingChoice = false;
                    target.Capture(); // 戦闘シーンに遷移するのでカメラ復帰は不要
                },
                onCancelCallback: () =>
                {
                    awaitingChoice = false;
                    target.Release();
                    RestoreFromAim();
                });
        }
        else
        {
            // パネルが無ければ即座に確保しておく（フォールバック）
            awaitingChoice = false;
            target.Capture();
        }
    }

    void RestoreFromAim()
    {
        if (thirdPersonFollow != null)
            thirdPersonFollow.CameraDistance = normalDistance;

        if (controller != null)
            controller.movementLocked = false;

        SetAvatarVisible(true);

        if (uiRoot != null)
            uiRoot.SetActive(true);

        if (crosshair != null)
            crosshair.SetActive(false);

        currentAimTarget?.SetAimHighlight(false);
        currentAimTarget = null;
    }

    void SetAvatarVisible(bool visible)
    {
        if (avatarRenderers == null) return;
        foreach (var r in avatarRenderers)
            if (r != null) r.enabled = visible;
    }
}
