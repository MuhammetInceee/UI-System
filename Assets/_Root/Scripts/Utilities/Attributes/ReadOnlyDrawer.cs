#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UISystem.Attributes
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]

    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
}
#endif