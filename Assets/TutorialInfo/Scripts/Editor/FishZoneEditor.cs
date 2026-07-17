using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FishZone))]
public class FishZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty databaseProp = serializedObject.FindProperty("fishDatabase");
        EditorGUILayout.PropertyField(databaseProp);

        serializedObject.ApplyModifiedProperties();

        FishZone zone = (FishZone)target;
        FishDatabase database = zone.Database;

        EditorGUILayout.Space();

        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign a FishDatabase first.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Allowed Fish", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Pick Tier, then Fish", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        int removeIndex = -1;

        for (int i = 0; i < zone.Entries.Count; i++)
        {
            FishZoneEntry entry = zone.Entries[i];

            EditorGUILayout.BeginHorizontal();

            FishTier newTier = (FishTier)EditorGUILayout.EnumPopup(entry.tier, GUILayout.Width(100));

            List<FishData> fishInTier = database.GetAll().Where(f => f.fishTier == newTier).ToList();

            if (fishInTier.Count == 0)
            {
                EditorGUILayout.LabelField($"No {newTier} fish");
            }
            else
            {
                string[] fishLabels = fishInTier.Select(f => f.fishName.ToString()).ToArray();

                int currentIndex = fishInTier.FindIndex(f => f.fishName == entry.fishName);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup(currentIndex, fishLabels);
                FishName newFishName = fishInTier[newIndex].fishName;

                if (newTier != entry.tier)
                {
                    Undo.RecordObject(zone, "Change Entry Tier");
                    zone.SetEntryTier(i, newTier);
                    zone.SetEntryFish(i, newFishName);
                    EditorUtility.SetDirty(zone);
                }
                else if (newFishName != entry.fishName)
                {
                    Undo.RecordObject(zone, "Change Entry Fish");
                    zone.SetEntryFish(i, newFishName);
                    EditorUtility.SetDirty(zone);
                }
            }

            if (GUILayout.Button("-", GUILayout.Width(24)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            Undo.RecordObject(zone, "Remove Entry");
            zone.RemoveEntry(removeIndex);
            EditorUtility.SetDirty(zone);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("+ Add Fish"))
        {
            FishData defaultFish = database.GetAll().FirstOrDefault(f => f.fishTier == FishTier.Common)
                                    ?? database.GetAll().FirstOrDefault();

            if (defaultFish != null)
            {
                Undo.RecordObject(zone, "Add Entry");
                zone.AddEntry(defaultFish.fishTier, defaultFish.fishName);
                EditorUtility.SetDirty(zone);
            }
            else
            {
                Debug.LogWarning("FishDatabase is empty");
            }
        }
    }
}
