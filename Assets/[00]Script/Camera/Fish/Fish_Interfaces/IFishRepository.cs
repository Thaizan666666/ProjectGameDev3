using System.Collections.Generic;

// Read access to fish master data
public interface IFishRepository
{
    IReadOnlyList<FishData> GetAll();
    IReadOnlyList<FishData> GetByTier(FishTier tier);
    FishData GetByName(FishName fishName);
}
