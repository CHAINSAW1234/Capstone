using UnityEditor;
using UnityEngine;
using static StartSceneUIManager;

[CustomPropertyDrawer(typeof(NamedString))]
public class NamedStringDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty itemProp = property.FindPropertyRelative("item");
        SerializedProperty modeProp = property.FindPropertyRelative("mode");
        SerializedProperty referenceProp = property.FindPropertyRelative("reference");
        SerializedProperty strProp = property.FindPropertyRelative("str");

        string enumLabel = $"{itemProp.enumDisplayNames[itemProp.enumValueIndex]} - {modeProp.enumDisplayNames[modeProp.enumValueIndex]}";
        label.text = enumLabel;

        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // 줄별 위치 계산
        Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect itemModeRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
        Rect referenceRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);

        // 라벨 표시
        EditorGUI.LabelField(labelRect, label);

        // item, mode 나란히
        float halfWidth = (position.width - 4) / 2f;
        Rect itemRect = new Rect(itemModeRect.x, itemModeRect.y, halfWidth, lineHeight);
        Rect modeRect = new Rect(itemModeRect.x + halfWidth + 4, itemModeRect.y, halfWidth, lineHeight);

        EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none);
        EditorGUI.PropertyField(modeRect, modeProp, GUIContent.none);
        EditorGUI.PropertyField(referenceRect, referenceProp, new GUIContent("Reference"));

        if (EditorGUI.EndChangeCheck())
        {
            if (referenceProp.objectReferenceValue != null)
            {
                strProp.stringValue = referenceProp.objectReferenceValue.name;
            }
        }
        EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return (lineHeight + spacing) * 3; // 3줄 분량
    }
}
