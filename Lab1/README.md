# Lab 1: SimpleRPC

The grain storage system with components connected through
[SimpleRpc](https://github.com/DaniilSokolyuk/SimpleRpc) over HTTP.

| Project   | Role | Depends on |
|-----------|------|------------|
| `Storage` | RPC server on `http://0.0.0.0:5000/simplerpc`. `StorageService` implements `IStorageService` and forwards every call to `StorageLogic` | `Shared/StorageContract`, `Shared/StorageCore` |
| `Baker`   | Client. Calls the server through a SimpleRpc proxy of `IStorageService` | `Shared/StorageContract` |
| `Farmer`  | Client. Same as Baker, deposits grain instead | `Shared/StorageContract` |

The clients read the server address from `STORAGE_URL`
(default `http://127.0.0.1:5000/simplerpc`).

All commands below are run from the **repository root** (`simplerpc/`).

## Run locally

```bash
dotnet run --project Lab1/Storage
```

In separate terminals (start as many as you want):
```bash
dotnet run --project Lab1/Baker
```
```bash
dotnet run --project Lab1/Farmer
```

## Docker images

The Dockerfiles use the repository root as build context, because they need `Shared/`:
```bash
docker build -f Lab1/Storage/Dockerfile -t jaguanas/simplerpc-storage:0.1.0 .
```
```bash
docker build -f Lab1/Baker/Dockerfile -t jaguanas/simplerpc-baker:0.1.0 .
```
```bash
docker build -f Lab1/Farmer/Dockerfile -t jaguanas/simplerpc-farmer:0.1.0 .
```

## Docker Compose

```bash
docker compose -f Lab1/Deployment/compose.yaml up -d
```
```bash
docker compose -f Lab1/Deployment/compose.yaml logs -f
```
```bash
docker compose -f Lab1/Deployment/compose.yaml down
```

Inside the Docker network the clients use `http://storage:5000/simplerpc`.

## Kubernetes (kind)

```bash
kind create cluster --name simplerpc
```
```bash
kubectl apply -f Lab1/Deployment/k8s/namespace.yaml
```
```bash
kubectl apply -f Lab1/Deployment/k8s/storage.yaml
```
```bash
kubectl apply -f Lab1/Deployment/k8s/baker.yaml
```
```bash
kubectl apply -f Lab1/Deployment/k8s/farmer.yaml
```

Logs:
```bash
kubectl logs -f deployment/storage -n simplerpc
```

Clean up:
```bash
kubectl delete namespace simplerpc
```
```bash
kind delete cluster --name simplerpc
```

## Terraform

Needs the `kind-simplerpc` cluster from above. The state files live in
`Lab1/Deployment/terraform/`, so always run Terraform from that folder:
```bash
cd Lab1/Deployment/terraform
```
```bash
terraform init
```
```bash
terraform plan
```
```bash
terraform apply
```
```bash
kubectl get all -n simplerpc-tf
```
```bash
terraform destroy
```
