# Grain storage

> A distributed systems course project at Kaunas University of Technology: one storage service built over four different transports (SimpleRPC, REST, gRPC and RabbitMQ), all sharing one core logic layer. The services are containerised with Docker and deployed to Kubernetes with both YAML manifests and Terraform.

**The system:** a Storage server keeps a grain balance and cycles through the seasons
(Winter → Spring → Summer → Autumn, every 15 s). **Bakers** take grain out
periodically (their period is 2× longer in winter and spring). If there is not enough grain,
the missed profit grows; otherwise the earned profit grows. **Farmers** work only
in summer and autumn and deposit grain. If the storage is full they wait, and if winter
arrives while they wait, their portion is lost.

## Folder layout

```
simplerpc/
├── simplerpc.sln          Opens everything; solution folders match the dirs below
│
├── Shared/                Used by every lab, so it lives in one place
│   ├── StorageContract/   IStorageService + data types (BakerDesc, FarmerDesc, Season, ...)
│   └── StorageCore/       StorageLogic: the business rules, thread-safe, season timer
│
├── Lab1/                  Lab 1: SimpleRPC
│   ├── Storage/           SimpleRPC server (port 5000) → StorageCore
│   ├── Baker/             SimpleRPC client (uses StorageContract proxy)
│   ├── Farmer/            SimpleRPC client (uses StorageContract proxy)
│   └── Deployment/        Docker Compose, Kubernetes, Terraform for the Lab 1 images
│
├── Lab2/                  Lab 2: web services (same system, two more transports)
│   ├── Rest/
│   │   ├── StorageRest/   ASP.NET Web API + Swagger/OpenAPI (port 5001) → StorageCore
│   │   ├── BakerRest/     client generated from the OpenAPI doc with NSwag
│   │   └── FarmerRest/    client generated from the OpenAPI doc with NSwag
│   └── Grpc/
│       ├── StorageGrpc/   gRPC server (port 5002) → StorageCore; owns Protos/storage.proto
│       ├── BakerGrpc/     client generated from storage.proto
│       └── FarmerGrpc/    client generated from storage.proto
│
└── Lab3/                  Lab 3: RabbitMQ message broker (request/reply over queues)
    ├── StorageRabbitTransport/   RPCMessage envelope, queue names, hand-written StorageClient
    ├── StorageRabbit/            server: consumes the request queue → StorageCore
    ├── BakerRabbit/              client loop using StorageClient
    └── FarmerRabbit/             client loop using StorageClient
```

How the pieces connect:

```
                 Shared/StorageContract  (types + IStorageService)
                            ▲
                 Shared/StorageCore      (StorageLogic, the rules)
          ┌─────────────────┼──────────────────┬───────────────────────────┐
   Lab1/Storage      Lab2/Rest/StorageRest   Lab2/Grpc/StorageGrpc   Lab3/StorageRabbit
   SimpleRPC :5000   REST :5001              gRPC :5002              RabbitMQ broker :5672
       ▲                    ▲                     ▲                        ▲
   Baker, Farmer     BakerRest, FarmerRest   BakerGrpc, FarmerGrpc   BakerRabbit, FarmerRabbit
```

The rules exist only once, in `StorageCore`. Each lab's server is a thin adapter that
exposes the same logic over its own transport. That is what keeps "the same system as
in Lab 1" true. In Lab 3 the clients and the server never connect to each other: they all
connect to the RabbitMQ broker and exchange request/reply messages through it.

## Build everything

```bash
dotnet build simplerpc.sln
```

Run instructions are in each lab's README:
- [Lab1/README.md](Lab1/README.md): SimpleRPC, Docker, Kubernetes, Terraform
- [Lab2/README.md](Lab2/README.md): REST and gRPC
- [Lab3/README.md](Lab3/README.md): RabbitMQ

## Requirements

- .NET 10 SDK
- Docker Desktop (Lab 1 deployment, and the RabbitMQ broker for Lab 3)
- kind, kubectl, Terraform (only for Lab 1 deployment)

This project is for educational purposes.
