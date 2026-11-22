namespace StorageCore;

using Services;

public interface IStorageLogic
{
    int RegisterBaker(BakerDesc baker);

    int RegisterFarmer(FarmerDesc farmer);

    Season GetSeasonState();

    GrainOperationResult ConsumeGrain(
        BakerDesc baker,
        int amount);

    GrainOperationResult TryDepositGrain(
        FarmerDesc farmer,
        int amount);
}