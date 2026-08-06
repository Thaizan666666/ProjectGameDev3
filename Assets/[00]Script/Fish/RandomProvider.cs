// Swappable random source, defaults to Unity's
public static class RandomProvider
{
    public static IRandomProvider Current { get; set; } = new UnityRandomProvider();
}
