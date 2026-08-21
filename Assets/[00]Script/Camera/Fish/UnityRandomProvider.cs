using UnityEngine;

// Default provider, wraps UnityEngine.Random
public class UnityRandomProvider : IRandomProvider
{
    public int Range(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    public float Range(float minInclusive, float maxInclusive) => Random.Range(minInclusive, maxInclusive);
}
