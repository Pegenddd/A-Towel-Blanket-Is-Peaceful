#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ChoicePanelAutoBuilder
{
    static ChoicePanelAutoBuilder()
    {
        EditorApplication.delayCall += EnsureChoicePanelInScene;
    }

    [MenuItem("Tools/Towel Blanket/Create ChoicePanel in Scene Now")]
    public static void EnsureChoicePanelInScene()
    {
        if (Application.isPlaying) return;

        DialogueManager dm = Object.FindFirstObjectByType<DialogueManager>();
        ChoicePanel cp = Object.FindFirstObjectByType<ChoicePanel>();

        if (cp == null)
        {
            Debug.Log("<color=yellow>[ChoicePanelAutoBuilder]</color> Generating ChoicePanel into Scene...");
            cp = ChoicePanel.CreateChoicePanelHierarchy();
            if (cp != null)
            {
                Debug.Log("<color=green>[ChoicePanelAutoBuilder]</color> ChoicePanel created in Canvas successfully!");
            }
        }

        if (dm != null && cp != null)
        {
            if (dm.choicePanel != cp)
            {
                Undo.RecordObject(dm, "Assign ChoicePanel to DialogueManager");
                dm.choicePanel = cp;
                EditorUtility.SetDirty(dm);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
