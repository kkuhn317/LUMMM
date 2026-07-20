#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.PropertyVariants;

public static class RepairBrokenLocalizers
{
    [MenuItem("Tools/Localization/Repair Broken GameObject Localizers")]
    private static void Repair()
    {
        GameObjectLocalizer[] localizers =
            Resources.FindObjectsOfTypeAll<GameObjectLocalizer>();

        int repairedComponents = 0;
        int removedEntries = 0;

        foreach (GameObjectLocalizer localizer in localizers)
        {
            if (localizer == null)
                continue;

            // Ignore prefab assets and other persistent project assets.
            if (EditorUtility.IsPersistent(localizer))
                continue;

            if (!localizer.gameObject.scene.IsValid())
                continue;

            SerializedObject serializedObject =
                new SerializedObject(localizer);

            serializedObject.Update();

            SerializedProperty trackedObjects =
                serializedObject.FindProperty("m_TrackedObjects");

            if (trackedObjects == null || !trackedObjects.isArray)
                continue;

            bool changed = false;

            for (int i = trackedObjects.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty entry =
                    trackedObjects.GetArrayElementAtIndex(i);

                bool invalid = entry.managedReferenceValue == null;

                if (!invalid)
                {
                    SerializedProperty target =
                        entry.FindPropertyRelative("m_Target");

                    invalid = target != null &&
                              target.objectReferenceValue == null;
                }

                if (!invalid)
                    continue;

                if (!changed)
                    Undo.RecordObject(
                        localizer,
                        "Repair Broken GameObject Localizer"
                    );

                int previousSize = trackedObjects.arraySize;

                trackedObjects.DeleteArrayElementAtIndex(i);

                // Some serialized reference arrays require a second deletion.
                if (trackedObjects.arraySize == previousSize)
                    trackedObjects.DeleteArrayElementAtIndex(i);

                changed = true;
                removedEntries++;
            }

            if (!changed)
                continue;

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(localizer);
            EditorSceneManager.MarkSceneDirty(localizer.gameObject.scene);

            if (PrefabUtility.IsPartOfPrefabInstance(localizer))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    localizer
                );
            }

            repairedComponents++;
        }

        Debug.Log(
            $"Localization repair complete. " +
            $"Repaired components: {repairedComponents}. " +
            $"Removed broken entries: {removedEntries}."
        );
    }
}

#endif