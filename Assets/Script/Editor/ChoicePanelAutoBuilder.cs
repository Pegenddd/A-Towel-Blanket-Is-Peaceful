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

    [MenuItem("Tools/Towel Blanket/Create ChoicePanel and AudioManager in Scene Now")]
    public static void EnsureChoicePanelInScene()
    {
        if (Application.isPlaying) return;

        DialogueManager dm = Object.FindFirstObjectByType<DialogueManager>();
        ChoicePanel cp = Object.FindFirstObjectByType<ChoicePanel>();
        AudioManager am = Object.FindFirstObjectByType<AudioManager>();

        if (cp == null)
        {
            Debug.Log("<color=yellow>[ChoicePanelAutoBuilder]</color> Generating ChoicePanel into Scene...");
            cp = ChoicePanel.CreateChoicePanelHierarchy();
            if (cp != null)
            {
                Debug.Log("<color=green>[ChoicePanelAutoBuilder]</color> ChoicePanel created in Canvas successfully!");
            }
        }

        if (am == null)
        {
            GameObject amObj = new GameObject("AudioManager");
            am = amObj.AddComponent<AudioManager>();
            am.EnsureAudioSources();
            Undo.RegisterCreatedObjectUndo(amObj, "Create AudioManager");
            Debug.Log("<color=green>[ChoicePanelAutoBuilder]</color> AudioManager created in Scene successfully!");
        }

        if (dm != null)
        {
            bool dirty = false;
            if (cp != null && dm.choicePanel != cp)
            {
                Undo.RecordObject(dm, "Assign ChoicePanel to DialogueManager");
                dm.choicePanel = cp;
                dirty = true;
            }
            if (am != null && dm.audioManager != am)
            {
                Undo.RecordObject(dm, "Assign AudioManager to DialogueManager");
                dm.audioManager = am;
                dirty = true;
            }
            if (dirty)
            {
                EditorUtility.SetDirty(dm);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
#endif
