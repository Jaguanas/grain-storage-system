namespace StorageRest.Controllers;

using Microsoft.AspNetCore.Mvc;
using Services;
using StorageCore;

[ApiController]
[Route("storage")]
public class StorageController : ControllerBase
{
    private readonly IStorageLogic mLogic;

    public StorageController(IStorageLogic logic)
    {
        mLogic = logic;
    }

    [HttpGet("season")]
    public ActionResult<Season> GetSeasonState()
    {
        return mLogic.GetSeasonState();
    }

    [HttpPost("bakers")]
    public ActionResult<int> RegisterBaker(BakerDesc baker)
    {
        return mLogic.RegisterBaker(baker);
    }

    [HttpPost("farmers")]
    public ActionResult<int> RegisterFarmer(FarmerDesc farmer)
    {
        return mLogic.RegisterFarmer(farmer);
    }

    [HttpPost("consume")]
    public ActionResult<GrainOperationResult> ConsumeGrain(
        BakerDesc baker,
        [FromQuery] int amount)
    {
        return mLogic.ConsumeGrain(baker, amount);
    }

    [HttpPost("produce")]
    public ActionResult<GrainOperationResult> TryDepositGrain(
        FarmerDesc farmer,
        [FromQuery] int amount)
    {
        return mLogic.TryDepositGrain(farmer, amount);
    }
}