// Something that can roll a random fish
public interface IRandomFishProvider
{
    FishData GetRandomFish();
    FishData GetRandomFish(FishTier tier);
}
