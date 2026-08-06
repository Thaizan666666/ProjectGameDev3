using System;
using System.Collections.Generic;
using UnityEngine;

// Weighted random pick, falls back to uniform if all weights are 0
public static class WeightedRandomPicker
{
    public static T Pick<T>(IReadOnlyList<T> candidates, Func<T, float> weightSelector)
    {
        if (candidates == null || candidates.Count == 0) return default;

        float totalWeight = 0f;
        foreach (T item in candidates)
            totalWeight += Mathf.Max(weightSelector(item), 0f);

        if (totalWeight <= 0f)
            return candidates[RandomProvider.Current.Range(0, candidates.Count)];

        float roll = RandomProvider.Current.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (T item in candidates)
        {
            cumulative += Mathf.Max(weightSelector(item), 0f);
            if (roll <= cumulative) return item;
        }

        return candidates[candidates.Count - 1];
    }
}
