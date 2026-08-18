using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Battle/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public int price = 100;    // 将来のショップ用（今回は未使用）
    public int healAmount = 0; // 0より大きければ使用時にHPを回復する
}
