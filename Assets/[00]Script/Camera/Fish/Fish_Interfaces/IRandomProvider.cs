// Abstraction over random generation (DIP)
public interface IRandomProvider
{
    int Range(int minInclusive, int maxExclusive);
    float Range(float minInclusive, float maxInclusive);
}
