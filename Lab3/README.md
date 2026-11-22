# Lab 3: Grain storage over RabbitMQ

The Lab 1 system (Storage, Baker, Farmer) reimplemented with message queues. Clients and the
server never connect to each other: all of them connect to the RabbitMQ broker and exchange
request/reply messages through it.

## Projects

| Project | Kind | Role |
|---|---|---|
| `StorageRabbitTransport` | library | `RPCMessage` envelope, `RabbitNames` (broker URI, exchange/queue names), `StorageClient : IStorageService` |
| `StorageRabbit` | exe | Server. Consumes requests, calls `IStorageLogic` (`Shared/StorageCore`), publishes replies |
| `BakerRabbit` | exe | Lab 1 baker loop using `new StorageClient()` |
| `FarmerRabbit` | exe | Lab 1 farmer loop using `new StorageClient()` |

`Shared/StorageContract` (the `IStorageService` contract) and `Shared/StorageCore` (the logic) are
reused unchanged. Everything RabbitMQ-specific lives in Lab 3 projects, so the contract and logic
don't depend on the networking mechanism.

## Running

Start RabbitMQ (management UI at http://localhost:15672, login guest/guest):

```powershell
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

Then, each in its own terminal, from the repository root:

```powershell
dotnet run --project Lab3/StorageRabbit
```

```powershell
dotnet run --project Lab3/BakerRabbit
```

```powershell
dotnet run --project Lab3/FarmerRabbit
```

Start as many bakers and farmers as you like. Set `RABBITMQ_URI` to use a broker somewhere else.

## Design decisions

- **Envelope location:** `RPCMessage` and the queue names are in `StorageRabbitTransport`, not in
  `Shared/StorageContract`. The contract is shared with Labs 1 and 2 and must not depend on the
  networking mechanism (requirement 6).
- **One `StorageClient`:** Baker and Farmer share one `StorageClient`, so `Call<T>` is written and
  fixed in one place. The server uses the same library only for the envelope and the queue names.
- **Request queue:** durable, so requests wait while the server is down. Both the server and the
  clients declare it, so a request sent before the server's first start isn't dropped.
- **Reply queue:** one per client, `exclusive`, so it's private and deleted when the client exits.
  Not `autoDelete`, because `Call<T>` unsubscribes after every call and the queue would disappear.
- **`autoAck: true`:** kept from the lecturer's example. If the server crashes mid-request, that
  request is lost and the client gets no reply. `autoAck: false` would redeliver it, but could apply
  it twice.
- **No timeout:** like the example, `Call<T>` waits forever, so a server crash mid-request blocks
  the client until it's restarted. The fix would be `WaitOne(timeout)` and then throwing, so the
  outer loop reconnects.
- **Stale requests:** requests from clients that have already exited still run when the server
  starts, and their replies are dropped. This affects at most one operation per client, and the
  rules are still checked against the current state.
- **Server concurrency:** callbacks run one at a time, but the `StorageLogic` lock is still needed,
  because the season thread changes the state at the same time.
- **Lab 1 fix:** the farmer's "Retrying in 2 sec..." message now matches its 2 s sleep. One retry
  message every 2 s per farmer is negligible load for the broker.
