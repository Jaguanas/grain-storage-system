namespace StorageGrpc;

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using StorageGrpc.Services;
using StorageCore;
using ContractBakerDesc = global::Services.BakerDesc;
using ContractFarmerDesc = global::Services.FarmerDesc;
using ContractGrainOperationResult = global::Services.GrainOperationResult;
using ContractSeason = global::Services.Season;

public sealed class StorageGrpcService : Storage.StorageBase
{
    private readonly IStorageLogic logic;

    public StorageGrpcService(IStorageLogic logic)
    {
        this.logic = logic;
    }

    public override Task<RegistrationResponse> RegisterBaker(BakerDesc request, ServerCallContext context)
    {
        var baker = new ContractBakerDesc { Id = request.Id, NameSurname = request.NameSurname };
        return Task.FromResult(new RegistrationResponse { Id = logic.RegisterBaker(baker) });
    }

    public override Task<RegistrationResponse> RegisterFarmer(FarmerDesc request, ServerCallContext context)
    {
        var farmer = new ContractFarmerDesc { Id = request.Id, NameSurname = request.NameSurname };
        return Task.FromResult(new RegistrationResponse { Id = logic.RegisterFarmer(farmer) });
    }

    public override Task<SeasonResponse> GetSeason(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new SeasonResponse
        {
            Season = (Season)(int)logic.GetSeasonState()
        });
    }

    public override Task<GrainOperationResponse> Consume(ConsumeRequest request, ServerCallContext context)
    {
        var baker = new ContractBakerDesc { Id = request.Baker.Id, NameSurname = request.Baker.NameSurname };
        return Task.FromResult(ToGrpcResult(logic.ConsumeGrain(baker, request.Amount)));
    }

    public override Task<GrainOperationResponse> Produce(ProduceRequest request, ServerCallContext context)
    {
        var farmer = new ContractFarmerDesc { Id = request.Farmer.Id, NameSurname = request.Farmer.NameSurname };
        return Task.FromResult(ToGrpcResult(logic.TryDepositGrain(farmer, request.Amount)));
    }

    private static GrainOperationResponse ToGrpcResult(ContractGrainOperationResult result)
    {
        return new GrainOperationResponse
        {
            Status = (GrainOperationStatus)(int)result.Status,
            Amount = result.Amount,
            Reason = result.Reason ?? string.Empty
        };
    }
}
