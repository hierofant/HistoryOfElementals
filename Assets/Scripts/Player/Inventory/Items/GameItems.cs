using UnityEngine;
using System.Collections.Generic;

namespace Game.Items
{
    public class GameItems : MonoBehaviour
    {
        private static List<ItemStack> itemStacks = new();

        private void Awake()
        {
            LoadItems();
        }

        public static ItemStack GetItem(int itemId)
        {
            foreach (ItemStack stack in itemStacks)
            {
                if (stack.Item.id == itemId)
                {
                    return stack;
                }
            }
            Debug.LogWarning($"Предмет с id = {itemId}, не найден");
            return null;
        }

        public static ItemStack CreateStackedItem(int itemId, int count)
        {
            ItemStack originalStack = GetItem(itemId);
            if (originalStack == null)
            {
                Debug.LogError($"Попытка создать предмет с несуществующим id: {itemId}");
                return null;
            }

            return new ItemStack(originalStack.Item, count);
        }

        private void LoadItems()
        {
            Item[] loadedItems = Resources.LoadAll<Item>("Items");
            itemStacks.Clear();

            foreach (var item in loadedItems)
            {
                itemStacks.Add(new ItemStack(item, 0)); // Начальные предметы загружаются с count=0
            }
        }
    }

    /// <summary>
    /// Класс, представляющий конкретный стак предмета в инвентаре.
    /// </summary>
    public class ItemStack
    {
        public Item Item { get; private set; }
        public int Count { get; set; }

        public ItemStack(Item item, int count)
        {
            Item = item;
            Count = Mathf.Clamp(count, 0, item.MaxCount);
        }
        public ItemStack()
        {
            Item = null;
            Count = 0;
        }

        public ItemStack Clone()
        {
            return new ItemStack(Item, Count);
        }
    }
}
