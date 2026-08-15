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

    [Header("撮影設定")]
    public float photoRange = 30f;
    public LayerMask photoLayer = ~0;

    [Header("効果音")]
    public AudioSource audioSource;
    public AudioClip cameraSetSE;  // 構え始めた時
    public AudioClip cameraShotSE; // 撮影した時

    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private StarterAssets.ThirdPersonController controller;
    private Renderer[] avatarRenderers;
    private float normalDistance;
    private bool isAiming = false;
    private IPhotographable currentAimTarget;

    void Start()
    {
        if (cmObject != null)
            thirdPersonFollow = cmObject.GetComponent<Cinemachine3rdPersonFollow>();

        controller = GetComponentInParent<StarterAssets.ThirdPersonController>();
        avatarRenderers = GetComponentsInChildren<Renderer>(true);

        if (crosshair != null)
            crosshair.SetActive(false);
    }

    void Update()
    {
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

        TakePhoto();

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

    void TakePhoto()
    {
        var target = FindPhotoTarget();
        target?.OnPhotographed();
    }
}
