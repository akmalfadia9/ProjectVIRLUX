using UnityEngine;

/// <summary>
/// KarakteristikReadOnlyAttribute
/// Attribute untuk menampilkan field sebagai read-only di Unity Inspector.
/// Digunakan oleh KarakteristikExperimentManager untuk debug state.
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
namespace KarakteristikEditor
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class KarakteristikReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif
