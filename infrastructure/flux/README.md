# FluxCD Setup Guide - Random Weather App

This guide explains the FluxCD setup for managing multiple environments (Staging and Production) with separate deployments.

## Architecture Overview

### Environments
- **Staging**: Runs on a dedicated worker node with minimal resources
- **Production**: Runs on another dedicated worker node with production-ready configuration

### Key Components
1. **FluxCD**: GitOps continuous deployment tool
2. **Helm Charts**: Templated Kubernetes manifests for UI, API, and Redis
3. **Redis Operator**: Manages Redis instances for both environments
4. **Traefik**: Ingress controller for routing

## Setup Instructions

### 1. Prerequisites

Install required tools:
```bash
# Install Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Install Flux CLI
curl -s https://fluxcd.io/install.sh | sudo bash

# Install Kind
go install sigs.k8s.io/kind@latest
```

### 2. Create Kind Cluster

```bash
kind create cluster --config=infrastructure/local/kind-config.yaml
```

This creates a cluster with:
- 1 control-plane node with ingress support
- 1 staging worker node (labeled: env=staging)
- 1 prod worker node (labeled: env=prod)

### 3. Install Traefik Ingress Controller

```bash
helm repo add traefik https://traefik.github.io/charts
helm install traefik traefik/traefik -n kube-system
```

### 4. Bootstrap FluxCD

Initialize FluxCD in your cluster:

```bash
# Export GitHub credentials (if using private repo)
export GITHUB_USER=<username>
export GITHUB_TOKEN=<token>

# Bootstrap Flux
flux bootstrap github \
  --owner=$GITHUB_USER \
  --repo=random-weather-app \
  --path=infrastructure/flux \
  --personal
```

Alternatively, for local development without bootstrapping to GitHub:

```bash
# Apply the flux manifests directly
kubectl apply -k infrastructure/flux/base
kubectl apply -k infrastructure/flux/environments/staging
kubectl apply -k infrastructure/flux/environments/prod
```

### 5. Verify Deployment

Check the status of deployments:

```bash
# Check Flux system
kubectl get pods -n flux-system

# Check staging environment
kubectl get pods -n staging
kubectl get svc -n staging
kubectl get ingress -n staging

# Check production environment
kubectl get pods -n prod
kubectl get svc -n prod
kubectl get ingress -n prod

# Watch HelmReleases
flux get helmreleases -A
flux logs -f
```

## Environment-Specific Configuration

### Staging Environment

**File**: `infrastructure/flux/environments/staging/`

- **Replicas**: 1 (minimal deployment)
- **Resources**:
  - Client: 50m CPU / 64Mi memory (request), 100m CPU / 128Mi memory (limit)
  - API: 50m CPU / 64Mi memory (request), 200m CPU / 256Mi memory (limit)
- **Redis**: Single-node, 1Gi storage, no replication
- **Ingress**: `angular-dotnet-web-app.stg.com.ua`
- **Caching**: Disabled on API
- **Node Selector**: `env: staging`

### Production Environment

**File**: `infrastructure/flux/environments/prod/`

- **Replicas**: 2 (initial), scales 2-5 via HPA
- **HPA Configuration**:
  - Min replicas: 2
  - Max replicas: 5
  - Target CPU: 80%
  - Target Memory: 80%
- **Resources**:
  - Client: 100m CPU / 128Mi memory (request), 500m CPU / 512Mi memory (limit)
  - API: 200m CPU / 256Mi memory (request), 1000m CPU / 1Gi memory (limit)
- **Redis**: HA setup with 3 replicas, Sentinel enabled, 10Gi storage
- **Ingress**: `angular-dotnet-web-app.com.ua`
- **Caching**: Enabled on API (600s TTL)
- **Node Selector**: `env: prod`

## Testing Deployments

### 1. Test Staging Environment

```bash
# Port forward to staging
kubectl port-forward -n staging svc/weather-frontend-service 3000:80 &
kubectl port-forward -n staging svc/weather-api-service 8080:8080 &

# Access application
curl http://localhost:3000
curl http://localhost:8080/health/live
```

### 2. Test Production Environment

```bash
# Port forward to prod
kubectl port-forward -n prod svc/weather-frontend-service 3001:80 &
kubectl port-forward -n prod svc/weather-api-service 8081:8080 &

# Access application
curl http://localhost:3001
curl http://localhost:8081/health/live
```

### 3. Test HPA in Production

```bash
# Generate load to trigger HPA
kubectl run -i --tty load-generator --rm --image=busybox --restart=Never -- \
  sh -c "while sleep 0.01; do wget -q -O- http://weather-api-service.prod.svc.cluster.local:8080/health/live; done"

# Watch HPA scaling
kubectl get hpa -n prod -w
```

## Redis Configuration

### Staging Redis
- **Type**: Single-node Redis
- **Replicas**: 1
- **Storage**: 1Gi
- **Purpose**: Development/testing
- **Address**: `redis-staging.staging.svc.cluster.local:6379`

### Production Redis
- **Type**: Redis with Sentinel
- **Master Replicas**: 3
- **Sentinel Replicas**: 3 (high availability)
- **Storage**: 10Gi
- **Caching**: Enabled (600s TTL)
- **Purpose**: Production with automatic failover
- **Address**: `redis-prod-leader.prod.svc.cluster.local:6379`

## Ingress Configuration

### Traefik Middleware

Both environments use Traefik middleware to strip the `/api` prefix:

```yaml
middleware:
  enabled: true
  name: strip-api-prefix
  prefixes:
    - /api
```

This allows requests to `/api/weather` to be forwarded to the API service as `/weather`.

### Hosts

- **Staging**: `angular-dotnet-web-app.stg.com.ua`
- **Production**: `angular-dotnet-web-app.com.ua`

To test locally, add entries to `/etc/hosts`:
```bash
127.0.0.1 angular-dotnet-web-app.stg.com.ua
127.0.0.1 angular-dotnet-web-app.com.ua
```

## Monitoring and Troubleshooting

### Check HelmRelease Status

```bash
# Get all releases
kubectl get helmrelease -A

# Describe specific release
kubectl describe helmrelease weather-api -n staging

# Check release history
helm history weather-api -n staging

# Get release values
helm get values weather-api -n staging
```

### View Flux Logs

```bash
# Follow Flux logs
flux logs -f

# Filter by component
flux logs -f --all-namespaces=true --kind=HelmRelease
flux logs -f --all-namespaces=true --kind=GitRepository
```

### Redis Status

```bash
# Check Redis pods
kubectl get pods -n staging -l app.kubernetes.io/name=redis
kubectl get pods -n prod -l app.kubernetes.io/name=redis

# Connect to Redis (staging)
kubectl exec -it -n staging redis-staging-0 -- redis-cli

# Check Redis replication status
redis-cli -p 6379 INFO replication
```

### Debugging Failed Deployments

```bash
# Check pod status
kubectl describe pod -n staging <pod-name>
kubectl logs -n staging <pod-name>

# Check HelmRelease status
flux events -n staging --kind HelmRelease

# Reconcile manually
flux reconcile helmrelease weather-api -n staging --with-source
```

## Common Tasks

### Update Image Version

Modify the `values.yaml` in the relevant environment:

```yaml
# For staging client
infrastructure/flux/environments/staging/configmap-client-values.yaml

# Update the image tag:
image:
  tag: "1.0.9"
```

Or use Flux image automation to automatically track new image tags.

### Scale Manually (disable HPA)

```bash
# Disable HPA
kubectl patch helmrelease weather-api -n prod \
  -p '{"spec":{"values":{"api":{"autoscaling":{"enabled":false,"replicaCount":3}}}}}' \
  --type merge

# Manually scale deployment
kubectl scale deployment weather-api-deployment -n prod --replicas=4
```

### Switch to Different Git Branch

```bash
# Update GitRepository source
kubectl patch gitrepository weather-app -n flux-system \
  -p '{"spec":{"ref":{"branch":"develop"}}}' \
  --type merge
```

## Best Practices

1. **Separate Configuration**: Keep environment-specific values in ConfigMaps
2. **Node Selection**: Use nodeSelectors to ensure workloads run on correct nodes
3. **Resource Limits**: Always set resource requests and limits
4. **Health Checks**: Both apps have readiness and liveness probes
5. **GitOps**: All changes go through git and are deployed by Flux
6. **Monitoring**: Monitor HPA metrics and Redis replication status

## Cleanup

To remove all deployments:

```bash
# Delete environments
kubectl delete -k infrastructure/flux/environments/staging
kubectl delete -k infrastructure/flux/environments/prod

# Delete cluster
kind delete cluster --name web-cluster
```

---

For more information, see:
- [FluxCD Documentation](https://fluxcd.io/)
- [Helm Documentation](https://helm.sh/docs/)
- [Redis Operator](https://github.com/spotahome/redis-operator)
