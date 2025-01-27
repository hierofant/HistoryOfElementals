using System;

namespace Game.Items
{
    [Serializable]
    public class ProjectileComponent
    {
        public int projectileDamage = 5;
        public float speed = 10f;
        public float range = 20f;
    }

    [Serializable]
    public class MeleeComponent
    {
        public int durability = 100;
        public int sharpness = 10;
        public float attackSpeed = 1f;
    }

    [Serializable]
    public class ShootingComponent
    {
        public int durability = 100;
        public int tension = 50;
        public float reloadTime = 1.5f;
    }

    [Serializable]
    public class FoodComponent
    {
        public int saturation = 10;
        public float consumeTime = 2f;
    }

    [Serializable]
    public class ArmorComponent
    {
        public int durability = 100;
        public float protectiont = 10;
    }

    [Serializable]
    public class AccessoryComponent
    {
    }
}