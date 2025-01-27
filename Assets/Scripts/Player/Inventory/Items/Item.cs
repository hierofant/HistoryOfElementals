using UnityEngine;
using System;

namespace Game.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item")]
    public class Item : ScriptableObject
    {
        public int MaxCount = 0;
        public int id = 0;
        public string displayName;
        public string systemName;
        public string description;
        public Sprite icon;
        public GameObject prefab;
        public ItemType type;

        // Компоненты предметов
        [SerializeField] private ProjectileComponent projectileComponent;
        [SerializeField] private MeleeComponent meleeComponent;
        [SerializeField] private ShootingComponent shootingComponent;
        [SerializeField] private FoodComponent foodComponent;
        [SerializeField] private ArmorComponent armorComponent;
        [SerializeField] private AccessoryComponent accessoryComponent;

        public T GetComponent<T>() where T : class
        {
            return type switch
            {
                ItemType.Projectile when typeof(T) == typeof(ProjectileComponent) => projectileComponent as T,
                ItemType.MeleeWeapon when typeof(T) == typeof(MeleeComponent) => meleeComponent as T,
                ItemType.ShootingWeapon when typeof(T) == typeof(ShootingComponent) => shootingComponent as T,
                ItemType.Armor when typeof(T) == typeof(ArmorComponent) => armorComponent as T,
                ItemType.Accessory when typeof(T) == typeof(AccessoryComponent) => accessoryComponent as T,
                ItemType.Food when typeof(T) == typeof(FoodComponent) => foodComponent as T,
                _ => null
            };
        }

        public Item Clone()
        {
            return (Item)this.MemberwiseClone();
        }

        private void OnValidate()
        {
            switch (type)
            {
                case ItemType.Projectile:
                    projectileComponent ??= new ProjectileComponent();
                    meleeComponent = null;
                    shootingComponent = null;
                    foodComponent = null;
                    accessoryComponent = null;
                    armorComponent = null;
                    break;
                case ItemType.MeleeWeapon:
                    projectileComponent = null;
                    meleeComponent ??= new MeleeComponent();
                    shootingComponent = null;
                    foodComponent = null;
                    accessoryComponent = null;
                    armorComponent = null;
                    break;
                case ItemType.ShootingWeapon:
                    projectileComponent = null;
                    meleeComponent = null;
                    shootingComponent ??= new ShootingComponent();
                    foodComponent = null;
                    accessoryComponent = null;
                    armorComponent = null;
                    break;
                case ItemType.Armor:
                    projectileComponent = null;
                    meleeComponent = null;
                    shootingComponent = null;
                    foodComponent = null;
                    accessoryComponent = null;
                    armorComponent ??= new ArmorComponent();
                    break;
                case ItemType.Accessory:
                    projectileComponent = null;
                    meleeComponent = null;
                    accessoryComponent ??= new AccessoryComponent();
                    foodComponent = null;
                    armorComponent = null;
                    shootingComponent = null;
                    break;
                case ItemType.Food:
                    projectileComponent = null;
                    meleeComponent = null;
                    accessoryComponent = null;
                    armorComponent = null;
                    shootingComponent = null;
                    foodComponent ??= new FoodComponent();
                    break;
            }
        }
    }
}