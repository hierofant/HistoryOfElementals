#if UNITY_EDITOR
using UnityEditor;

namespace Game.Items
{
    [CustomEditor(typeof(Item))]
    public class ItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Item item = (Item)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("systemName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));

            switch (item.type)
            {
                case ItemType.Projectile:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileComponent"));
                    break;
                case ItemType.MeleeWeapon:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("meleeComponent"));
                    break;
                case ItemType.ShootingWeapon:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shootingComponent"));
                    break;
                case ItemType.Armor:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("armorComponent"));
                    break;
                case ItemType.Accessory:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("accessoryComponent"));
                    break;
                case ItemType.Food:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("foodComponent"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif