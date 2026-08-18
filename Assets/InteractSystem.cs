using UnityEngine;

public class InteractSystem : MonoBehaviour
{
    [Header("設定")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("参照")]
    public InspectUI inspectUI;
    public Transform rayOrigin;

[Header("効果音")]
public AudioSource audioSource;
public AudioClip interactSE;

    private InteractableObject currentTarget;
    private bool isReading = false;

    // StarterAssetsのInputを取得
    private StarterAssets.StarterAssetsInputs playerInput;

    void Start()
    {
        playerInput = GetComponentInParent<StarterAssets.StarterAssetsInputs>();
    }

    void Update()
    {
        if (!isReading)
            DetectTarget();

        if (Input.GetKeyDown(KeyCode.F))
            OnPressF();
    }

    void DetectTarget()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            InteractableObject obj = hit.collider.GetComponent<InteractableObject>();
            if (obj != null)
            {
                if (currentTarget != obj)
{
    currentTarget = obj;

    if (obj.isShop)
        inspectUI.ShowHintText("購入");
    else if (obj.isNPC)
        inspectUI.ShowHintText("話す");
    else
        inspectUI.ShowHintText("調べる");
}
                return;
            }
        }

        if (currentTarget != null)
        {
            currentTarget = null;
            inspectUI.ShowHint(false);
        }
    }

void OnPressF()
{
    // F押下時SE
    if (audioSource != null && interactSE != null)
        audioSource.PlayOneShot(interactSE);

 if (!isReading && currentTarget != null)
    {
        // ★ 敵エンカウント用分岐
        if (currentTarget.isEnemy)
        {
            inspectUI.ShowHint(false);
            LockPlayer(true);
            BattleChoiceUI.Instance.Show(currentTarget, () =>
            {
                // やめるを押したときの処理
                LockPlayer(false);
                if (currentTarget != null)
                    inspectUI.ShowHintText("話す");
            });
            return; // 通常のテキスト表示はしない
        }

        // ★ お店（自販機など）用分岐
        if (currentTarget.isShop)
        {
            inspectUI.ShowHint(false);
            LockPlayer(true);
            ShopUI.Instance.Show(currentTarget.shopItemIds, () =>
            {
                LockPlayer(false);
                if (currentTarget != null)
                    inspectUI.ShowHintText("購入");
            });
            return; // 通常のテキスト表示はしない
        }

        isReading = true;
        inspectUI.ShowHint(false);
        inspectUI.StartReading(
    currentTarget.pages,
    currentTarget.isNPC,
    currentTarget.speakerName,
    currentTarget.voiceClips
);
        LockPlayer(true);
    }
   else if (isReading)
{
    bool finished = inspectUI.NextPage();
    if (finished)
    {
        isReading = false;

        if (currentTarget != null)
        {
            if (currentTarget.isNPC)
                inspectUI.ShowHintText("話す");
            else
                inspectUI.ShowHintText("調べる");
        }

        LockPlayer(false);
    }
}
}

  void LockPlayer(bool locked)
{
    var controller = GetComponentInParent<StarterAssets.ThirdPersonController>();
    if (controller != null)
        controller.movementLocked = locked;

    // 視点固定
    if (controller != null)
        controller.LockCameraPosition = locked;

    Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
    Cursor.visible = locked;
}
}