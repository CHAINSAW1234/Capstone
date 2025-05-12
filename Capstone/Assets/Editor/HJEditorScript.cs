using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static StartSceneUIManager;

[CustomEditor(typeof(List<SceneGroupEntry>))]
public class SceneDataManagerEditor : Editor
{
    private SerializedProperty sceneGroups;

    void OnEnable()
    {
        sceneGroups = serializedObject.FindProperty("sceneGroups");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Scene Group 설정", EditorStyles.boldLabel);

        for (int i = 0; i < sceneGroups.arraySize; i++)
        {
            SerializedProperty entry = sceneGroups.GetArrayElementAtIndex(i);
            SerializedProperty item = entry.FindPropertyRelative("item");
            SerializedProperty scenes = entry.FindPropertyRelative("scenes");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            item.enumValueIndex = (int)(StudyItem)EditorGUILayout.EnumPopup("StudyItem", (StudyItem)item.enumValueIndex);
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                sceneGroups.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(scenes, new GUIContent("Scene 리스트"), true);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("새 SceneGroup 추가"))
        {
            sceneGroups.arraySize++;
        }

        serializedObject.ApplyModifiedProperties();
    }
}


[CustomPropertyDrawer(typeof(NamedString))]
public class NamedStringDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty referenceProp = property.FindPropertyRelative("reference");
        SerializedProperty strProp = property.FindPropertyRelative("str");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect referenceRect = new Rect(position.x, position.y, position.width, lineHeight);

        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(referenceRect, referenceProp, label);

        if (EditorGUI.EndChangeCheck())
        {
            if (referenceProp.objectReferenceValue != null)
            {
                strProp.stringValue = referenceProp.objectReferenceValue.name;
            }
        }

        EditorGUI.EndProperty();
    }
}
