using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Battle/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    // MenuController等、コード側から生成したオブジェクトからも参照できるよう
    // Assets/Resources に置いたアセットを遅延ロードして共有する
    private static ItemDatabase instance;
    public static ItemDatabase Instance =>
        instance != null ? instance : (instance = Resources.Load<ItemDatabase>("ItemDatabase"));

    public List<ItemDefinition> items = new List<ItemDefinition>();

    public ItemDefinition Find(string itemId) => items.Find(i => i.itemId == itemId);
}
