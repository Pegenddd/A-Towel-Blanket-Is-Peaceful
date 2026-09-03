#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueElementSelectorAttribute))]
public class DialogueElementSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        SerializedProperty dialoguesProp = property.serializedObject.FindProperty("dialogues");
        int count = 0;
        if (dialoguesProp != null && dialoguesProp.isArray)
        {
            count = dialoguesProp.arraySize;
        }
        else
        {
            DialogueManager dm = Object.FindFirstObjectByType<DialogueManager>();
            if (dm != null && dm.dialogues != null)
            {
                count = dm.dialogues.Length;
            }
        }

        if (count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string[] options = new string[count + 1];
        int[] values = new int[count + 1];

        options[0] = "-1: (Next Element in List)";
        values[0] = -1;

        for (int i = 0; i < count; i++)
        {
            string snippet = "";
            if (dialoguesProp != null && i < dialoguesProp.arraySize)
            {
                SerializedProperty elem = dialoguesProp.GetArrayElementAtIndex(i);
                SerializedProperty textProp = elem != null ? elem.FindPropertyRelative("text") : null;
                if (textProp != null && !string.IsNullOrEmpty(textProp.stringValue))
                {
                    snippet = textProp.stringValue;
                }
            }
            else
            {
                DialogueManager dm = Object.FindFirstObjectByType<DialogueManager>();
                if (dm != null && dm.dialogues != null && i < dm.dialogues.Length && dm.dialogues[i] != null)
                {
                    snippet = dm.dialogues[i].text;
                }
            }

            snippet = !string.IsNullOrEmpty(snippet)
                ? (snippet.Length > 22 ? snippet.Substring(0, 22) + "..." : snippet).Replace("\n", " ")
                : "(Empty)";

            options[i + 1] = $"Element {i}: \"{snippet}\"";
            values[i + 1] = i;
        }

        EditorGUI.BeginProperty(position, label, property);
        int currentVal = property.intValue;
        int newVal = EditorGUI.IntPopup(position, label.text, currentVal, options, values);
        if (newVal != currentVal)
        {
            property.intValue = newVal;
        }
        EditorGUI.EndProperty();
    }
}
#endif
