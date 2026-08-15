using UnityEngine;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    public float checkDistance = 2f;
    public LayerMask interactLayer;

    public GameObject hintUI;
    public TextMeshProUGUI messageText;

    Interactable currentTarget;

    void Update()
    {
        CheckObject();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.F))
        {
            ShowMessage();
        }
    }

    void CheckObject()
    {
        Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, checkDistance, interactLayer))
        {
            Interactable obj = hit.collider.GetComponentInParent<Interactable>();

            if (obj != null)
            {
                currentTarget = obj;
                hintUI.SetActive(true);
                return;
            }
        }

        currentTarget = null;
        hintUI.SetActive(false);
    }

    void ShowMessage()
    {
        messageText.gameObject.SetActive(true);
        messageText.text = currentTarget.message;
    }
}