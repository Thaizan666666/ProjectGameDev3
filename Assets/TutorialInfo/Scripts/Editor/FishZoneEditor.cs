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

        database.EnsureLoaded();

        List<FishData> allFish = database.GetAll().ToList();

        if (allFish.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "FishDatabase has no fish.\n" +
                "Assign FishStats assets to FishDatabase's 'Fish Stats Assets' array.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Allowed Fish  ({allFish.Count} species in DB)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Pick Tier, then Fish", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        int removeIndex = -1;

        for (int i = 0; i < zone.Entries.Count; i++)
        {
            FishZoneEntry entry = zone.Entries[i];

            EditorGUILayout.BeginHorizontal();

            FishTier newTier = (FishTier)EditorGUILayout.EnumPopup(entry.tier, GUILayout.Width(100));

            List<FishData> fishInTier = allFish.Where(f => f.fishTier == newTier).ToList();

            if (fishInTier.Count == 0)
            {
                EditorGUILayout.LabelField($"No {newTier} fish in DB");
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
            FishData defaultFish = allFish.FirstOrDefault(f => f.fishTier == FishTier.Common)
                                  ?? allFish.FirstOrDefault();

            if (defaultFish != null)
            {
                Undo.RecordObject(zone, "Add Entry");
                zone.AddEntry(defaultFish.fishTier, defaultFish.fishName);
                EditorUtility.SetDirty(zone);
            }
        }
    }
}