# CleanSync Deployment Guide

This guide covers multiple deployment options for CleanSync, from local Docker containers to production-grade Kubernetes and Azure deployments.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (for containerized deployments)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (for Kubernetes)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for Azure deployments)
- Azure subscription (for Azure deployments)

---

## Option 1: Docker Deployment

### Dockerfile (API)

Create a `Dockerfile` in the solution root:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY CleanSync.slnx .
COPY src/CleanSync.Api/CleanSync.Api.csproj src/CleanSync.Api/
COPY src/CleanSync.Application/CleanSync.Application.csproj src/CleanSync.Application/
COPY src/CleanSync.Domain/CleanSync.Domain.csproj src/CleanSync.Domain/
COPY src/CleanSync.Infrastructure/CleanSync.Infrastructure.csproj src/CleanSync.Infrastructure/
COPY src/CleanSync.Web/CleanSync.Web.csproj src/CleanSync.Web/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build
RUN dotnet publish src/CleanSync.Api/CleanSync.Api.csproj -c Release -o /app/publish --no-self-contained

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT dotnet CleanSync.Api.dll
```

### Dockerfile (Web)

Create a `Dockerfile.web` for the Blazor Web application:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY CleanSync.slnx .
COPY src/CleanSync.Api/CleanSync.Api.csproj src/CleanSync.Api/
COPY src/CleanSync.Application/CleanSync.Application.csproj src/CleanSync.Application/
COPY src/CleanSync.Domain/CleanSync.Domain.csproj src/CleanSync.Domain/
COPY src/CleanSync.Infrastructure/CleanSync.Infrastructure.csproj src/CleanSync.Infrastructure/
COPY src/CleanSync.Web/CleanSync.Web.csproj src/CleanSync.Web/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build
RUN dotnet publish src/CleanSync.Web/CleanSync.Web.csproj -c Release -o /app/publish --no-self-contained

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT dotnet CleanSync.Web.dll
```

### Docker Compose (Development)

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  cleansync-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - '5000:5000'
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - UseInMemoryDb=true
      - DemoMode=true
    depends_on:
      - sqlserver
    networks:
      - cleansync-network

  cleansync-web:
    build:
      context: .
      dockerfile: Dockerfile.web
    ports:
      - '5001:5001'
    environment:
      - ApiBaseUrl=http://cleansync-api:5000
    depends_on:
      - cleansync-api
    networks:
      - cleansync-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Passw0rd
      - MSSQL_PID=Developer
    ports:
      - '1433:1433'
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - cleansync-network

networks:
  cleansync-network:
    driver: bridge

volumes:
  sqlserver-data:
```

### Build and Run

```bash
# Build the image
docker build -t cleansync-api:latest .

# Run with docker-compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Docker Compose (Production)

Create `docker-compose.prod.yml`:

```yaml
version: '3.8'

services:
  cleansync-api:
    image: yourregistry.azurecr.io/cleansync-api:${TAG:-latest}
    restart: always
    ports:
      - '80:5000'
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - UseInMemoryDb=false
      - DemoMode=false
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=CleanSync;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True
      - SapConnection__ServiceLayerUrl=${SAP_SERVICE_LAYER_URL}
      - SapConnection__CompanyDb=${SAP_COMPANY_DB}
      - SapConnection__UserName=${SAP_USERNAME}
      - SapConnection__Password=${SAP_PASSWORD}
    secrets:
      - db_password
      - sap_password
    healthcheck:
      test: ['CMD', 'curl', '-f', 'http://localhost:5000/health']
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - cleansync-network

  cleansync-web:
    image: yourregistry.azurecr.io/cleansync-web:${TAG:-latest}
    restart: always
    ports:
      - '8080:5000'
    environment:
      - ApiBaseUrl=http://cleansync-api:5000
    depends_on:
      - cleansync-api
    networks:
      - cleansync-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    restart: always
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_PID=Standard
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - cleansync-network

networks:
  cleansync-network:
    driver: bridge

volumes:
  sqlserver-data:

secrets:
  db_password:
    file: ./secrets/db_password.txt
  sap_password:
    file: ./secrets/sap_password.txt
```

---

## Option 2: Kubernetes Deployment

### Namespace

Create a namespace for CleanSync:

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: cleansync
  labels:
    app.kubernetes.io/name: cleansync
```

### ConfigMap

```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: cleansync-config
  namespace: cleansync
data:
  ASPNETCORE_ENVIRONMENT: 'Production'
  UseInMemoryDb: 'false'
  DemoMode: 'false'
```

### Secrets

```yaml
# k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: cleansync-secrets
  namespace: cleansync
type: Opaque
stringData:
  DB_CONNECTION_STRING: 'Server=sqlserver.default.svc.cluster.local;Database=CleanSync;User Id=sa;Password=YourPassword;TrustServerCertificate=True'
  SAP_SERVICE_LAYER_URL: 'https://your-sap-server:50000/b1s/v1'
  SAP_COMPANY_DB: 'YOURCOMPANY'
  SAP_USERNAME: 'manager'
  SAP_PASSWORD: 'your-password'
```

### API Deployment

```yaml
# k8s/api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cleansync-api
  namespace: cleansync
  labels:
    app: cleansync-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: cleansync-api
  template:
    metadata:
      labels:
        app: cleansync-api
    spec:
      containers:
        - name: cleansync-api
          image: yourregistry.azurecr.io/cleansync-api:latest
          ports:
            - containerPort: 5000
          env:
            - name: ASPNETCORE_ENVIRONMENT
              valueFrom:
                configMapKeyRef:
                  name: cleansync-config
                  key: ASPNETCORE_ENVIRONMENT
            - name: UseInMemoryDb
              valueFrom:
                configMapKeyRef:
                  name: cleansync-config
                  key: UseInMemoryDb
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: cleansync-secrets
                  key: DB_CONNECTION_STRING
            - name: SapConnection__ServiceLayerUrl
              valueFrom:
                secretKeyRef:
                  name: cleansync-secrets
                  key: SAP_SERVICE_LAYER_URL
            - name: SapConnection__CompanyDb
              valueFrom:
                secretKeyRef:
                  name: cleansync-secrets
                  key: SAP_COMPANY_DB
            - name: SapConnection__UserName
              valueFrom:
                secretKeyRef:
                  name: cleansync-secrets
                  key: SAP_USERNAME
            - name: SapConnection__Password
              valueFrom:
                secretKeyRef:
                  name: cleansync-secrets
                  key: SAP_PASSWORD
          resources:
            requests:
              cpu: '100m'
              memory: '256Mi'
            limits:
              cpu: '500m'
              memory: '512Mi'
          livenessProbe:
            httpGet:
              path: /health/live
              port: 5000
            initialDelaySeconds: 10
            periodSeconds: 30
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 5000
            initialDelaySeconds: 5
            periodSeconds: 10
```

### Web Deployment

```yaml
# k8s/web-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cleansync-web
  namespace: cleansync
  labels:
    app: cleansync-web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: cleansync-web
  template:
    metadata:
      labels:
        app: cleansync-web
    spec:
      containers:
        - name: cleansync-web
          image: yourregistry.azurecr.io/cleansync-web:latest
          ports:
            - containerPort: 5000
          env:
            - name: ApiBaseUrl
              value: 'http://cleansync-api:5000'
          resources:
            requests:
              cpu: '100m'
              memory: '256Mi'
            limits:
              cpu: '500m'
              memory: '512Mi'
          livenessProbe:
            httpGet:
              path: /health
              port: 5000
            initialDelaySeconds: 15
            periodSeconds: 30
          readinessProbe:
            httpGet:
              path: /health
              port: 5000
            initialDelaySeconds: 5
            periodSeconds: 10
```

### Horizontal Pod Autoscaler

```yaml
# k8s/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: cleansync-api-hpa
  namespace: cleansync
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: cleansync-api
  minReplicas: 1
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 10
          periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
        - type: Percent
          value: 100
          periodSeconds: 15
```

### Services

```yaml
# k8s/services.yaml
apiVersion: v1
kind: Service
metadata:
  name: cleansync-api
  namespace: cleansync
spec:
  type: ClusterIP
  ports:
    - port: 5000
      targetPort: 5000
  selector:
    app: cleansync-api
---
apiVersion: v1
kind: Service
metadata:
  name: cleansync-web
  namespace: cleansync
spec:
  type: LoadBalancer
  ports:
    - port: 80
      targetPort: 5000
  selector:
    app: cleansync-web
```

### Ingress

```yaml
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: cleansync-ingress
  namespace: cleansync
  annotations:
    kubernetes.io/ingress.class: 'nginx'
    cert-manager.io/cluster-issuer: 'letsencrypt-prod'
spec:
  tls:
    - hosts:
        - cleansync.yourdomain.com
      secretName: cleansync-tls
  rules:
    - host: cleansync.yourdomain.com
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: cleansync-web
                port:
                  number: 80
```

### Deploy to Kubernetes

```bash
# Apply all resources
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n cleansync

# View logs
kubectl logs -n cleansync -l app=cleansync-api

# Scale replicas
kubectl scale deployment cleansync-api --replicas=3 -n cleansync

# Watch rollout status
kubectl rollout status deployment/cleansync-api -n cleansync

# Rollback to previous version
kubectl rollout undo deployment/cleansync-api -n cleansync

# Rollback to specific revision
kubectl rollout undo deployment/cleansync-api --to-revision=2 -n cleansync

# Check rollout history
kubectl rollout history deployment/cleansync-api -n cleansync
```

---

## Option 3: Azure Deployment

### Option 3A: Azure App Service (Simplest)

#### Deploy with Azure CLI

```bash
# Login to Azure
az login

# Create resource group
az group create --name cleansync-rg --location eastus

# Create App Service Plan
az appservice plan create --name cleansync-plan --resource-group cleansync-rg --sku B1 --is-linux

# Create Web App for API
az webapp create --name cleansync-api --resource-group cleansync-rg --plan cleansync-plan --runtime 'DOTNET|10.0'

# Configure App Settings
az webapp config appsettings set --name cleansync-api --resource-group cleansync-rg --settings ^
  ASPNETCORE_ENVIRONMENT=Production ^
  UseInMemoryDb=false ^
  DemoMode=false ^
  ConnectionStrings__DefaultConnection='Server=tcp:cleansync-sql.database.windows.net;Database=CleanSync;User Id=adminuser;Password=YourPassword;TrustServerCertificate=False' ^
  SapConnection__ServiceLayerUrl='https://your-sap-server:50000/b1s/v1' ^
  SapConnection__CompanyDb='YOURCOMPANY' ^
  SapConnection__UserName='manager' ^
  SapConnection__Password='your-password'

# Deploy from GitHub Actions (create workflow in .github/workflows/deploy.yml)
```

#### Azure App Service Configuration (azure-appservice.yml)

```yaml
# azure-appservice.yml
resourceGroupName: cleansync-rg
appServicePlanName: cleansync-plan
webAppName: cleansync-api
slotName: staging
settings:
  - name: ASPNETCORE_ENVIRONMENT
    value: Production
  - name: UseInMemoryDb
    value: false
  - name: DemoMode
    value: false
  - name: ConnectionStrings__DefaultConnection
    value: '@Microsoft.KeyVault(SecretUri=https://cleansync-kv.vault.azure.net/secrets/db-connection-string/)'
  - name: SapConnection__ServiceLayerUrl
    value: '@Microsoft.KeyVault(SecretUri=https://cleansync-kv.vault.azure.net/secrets/sap-service-url/)'
```

### Option 3B: Azure Container Apps

```bash
# Create container apps environment
az containerapp env create --name cleansync-env --resource-group cleansync-rg --location eastus

# Create API container app
az containerapp create --name cleansync-api --resource-group cleansync-rg --environment cleansync-env ^
  --image yourregistry.azurecr.io/cleansync-api:latest ^
  --target-port 5000 ^
  --ingress external ^
  --cpu 0.25 --memory 0.5Gi ^
  --min-replicas 1 --max-replicas 3 ^
  --set-env-vars ^
    ASPNETCORE_ENVIRONMENT=Production ^
    UseInMemoryDb=false ^
    DemoMode=false ^
    ConnectionStrings__DefaultConnection='Server=tcp:cleansync-sql.database.windows.net;Database=CleanSync;User Id=adminuser;Password=YourPassword' ^
  --secrets ^
    sap-service-url='https://your-sap-server:50000/b1s/v1' ^
    sap-password='your-password'

# Create Web container app
az containerapp create --name cleansync-web --resource-group cleansync-rg --environment cleansync-env ^
  --image yourregistry.azurecr.io/cleansync-web:latest ^
  --target-port 5000 ^
  --ingress external ^
  --cpu 0.25 --memory 0.5Gi ^
  --min-replicas 1 --max-replicas 3 ^
  --set-env-vars ^
    ApiBaseUrl='https://cleansync-api.eastus.azurecontainerapps.io'

# Configure autoscaling
az containerapp update --name cleansync-api --resource-group cleansync-rg --min-replicas 1 --max-replicas 10 --scale-rule-name http-scaling --scale-rule-http-concurrency 50
```

### Option 3C: Azure Kubernetes Service (AKS)

```bash
# Create AKS cluster
az aks create --resource-group cleansync-rg --name cleansync-aks --node-count 2 --enable-addons monitoring --generate-ssh-keys

# Get credentials
az aks get-credentials --resource-group cleansync-rg --name cleansync-aks

# Create Azure Container Registry
az acr create --resource-group cleansync-rg --name cleansyncacr --sku Standard

# Login to ACR
az acr login --name cleansyncacr

# Tag images
docker tag cleansync-api:latest cleansyncacr.azurecr.io/cleansync-api:latest
docker tag cleansync-web:latest cleansyncacr.azurecr.io/cleansync-web:latest

# Push images
docker push cleansyncacr.azurecr.io/cleansync-api:latest
docker push cleansyncacr.azurecr.io/cleansync-web:latest

# Create Kubernetes secret for ACR
kubectl create secret docker-registry acr-secret --docker-server=cleansyncacr.azurecr.io --docker-username=cleansyncacr --docker-password=$(az acr credential show --name cleansyncacr --query passwords[0].value -o tsv) --namespace=cleansync

# Deploy using kubectl
kubectl apply -f k8s/
```

### Azure SQL Database Setup

```bash
# Create Azure SQL Server
az sql server create --name cleansync-sqlserver --resource-group cleansync-rg --location eastus --admin-user adminuser

# Create firewall rule for Azure services
az sql server firewall-rule create --resource-group cleansync-rg --server cleansync-sqlserver --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Create database
az sql db create --resource-group cleansync-rg --server cleansync-sqlserver --name CleanSync --service-objective S0

# Enable vulnerability assessment
az sql db vulnerability-assessment-rule-baseline enable --resource-group cleansync-rg --server cleansync-sqlserver --database-name CleanSync --rule-id VA2065

# Get connection string
az sql db show-connection-string --name CleanSync --server cleansync-sqlserver --client ado-net
```

### Azure Key Vault

```bash
# Create Key Vault
az keyvault create --name cleansync-kv --resource-group cleansync-rg --location eastus

# Add secrets
az keyvault secret set --vault-name cleansync-kv --name db-connection-string --value 'Server=tcp:cleansync-sqlserver.database.windows.net;Database=CleanSync;User Id=adminuser;Password=YourPassword'

az keyvault secret set --vault-name cleansync-kv --name sap-password --value 'your-sap-password'

# Enable managed identity for App Service
az webapp identity assign --name cleansync-api --resource-group cleansync-rg

# Grant Key Vault access
az keyvault set-policy --name cleansync-kv --object-id <identity-principal-id> --secret-permissions get list
```

---

### Security Note for Kubernetes Secrets

For production deployments, avoid storing secrets directly in Kubernetes Secret objects (base64 encoding is not encryption). Consider these alternatives:

- **Sealed Secrets**: Use [Bitnami Sealed Secrets](https://github.com/bitnami-labs/sealed-secrets) to encrypt secrets at rest
- **External Secrets Operator**: Sync secrets from AWS Secrets Manager, Azure Key Vault, or HashiCorp Vault
- **Vault Integration**: Use [HashiCorp Vault](https://www.vaultproject.io/) with the Vault Agent Injector

```yaml
# Example: External Secrets Operator with Azure Key Vault
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: cleansync-secrets
  namespace: cleansync
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: azure-key-vault
    kind: SecretStore
  target:
    name: cleansync-secrets
  data:
    - secretKey: SAP_PASSWORD
      remoteRef:
        key: sap-password
        property: value
```

---

## CI/CD Pipelines

### GitHub Actions (Deploy to Azure Container Registry + App Service)

```yaml
# .github/workflows/deploy.yml
name: Build and Deploy CleanSync

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  REGISTRY: yourregistry.azurecr.io
  IMAGE_NAME_API: cleansync-api
  IMAGE_NAME_WEB: cleansync-web
  DOTNET_VERSION: '10.0.x'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Login to Azure Container Registry
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Build and push API image
        run: |
          docker build -t ${{ env.REGISTRY }}/${{ env.IMAGE_NAME_API }}:${{ github.sha }} -f Dockerfile .
          docker push ${{ env.REGISTRY }}/${{ env.IMAGE_NAME_API }}:${{ github.sha }}
      
      - name: Build and push Web image
        run: |
          docker build -t ${{ env.REGISTRY }}/${{ env.IMAGE_NAME_WEB }}:${{ github.sha }} -f Dockerfile.web .
          docker push ${{ env.REGISTRY }}/${{ env.IMAGE_NAME_WEB }}:${{ github.sha }}
      
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: cleansync-api
          images: |
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME_API }}:${{ github.sha }}
```

```yaml
# .github/workflows/deploy.yml
name: Deploy CleanSync

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_WEBAPP_NAME: cleansync-api
  AZURE_WEBAPP_PACKAGE_PATH: './src/CleanSync.Api/publish'
  DOTNET_VERSION: '10.0.x'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Test
        run: dotnet test --configuration Release --no-build --verbosity normal
      
      - name: Publish
        run: dotnet publish ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}/CleanSync.Api.csproj -c Release -o ./publish
      
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: webapp
          path: ./publish

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment: production
    
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: webapp
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: .
```

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '10.0.x'

stages:
  - stage: Build
    jobs:
      - job: BuildAndTest
        steps:
          - task: UseDotNet@2
            displayName: 'Use .NET 10'
            inputs:
              packageType: 'sdk'
              version: $(dotnetVersion)
          
          - script: dotnet restore
            displayName: 'Restore packages'
          
          - script: dotnet build --configuration $(buildConfiguration)
            displayName: 'Build'
          
          - script: dotnet test --configuration $(buildConfiguration) --collect:'XPlat Code Coverage'
            displayName: 'Test'
          
          - task: PublishBuildArtifacts@1
            inputs:
              pathtoPublish: '$(Build.ArtifactStagingDirectory)'
              artifactName: 'drop'

  - stage: Deploy
    condition: succeeded()
    jobs:
      - deployment: DeployWebApp
        environment: 'production'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebApp@1
                  inputs:
                    azureSubscription: 'AzureServiceConnection'
                    appType: 'webApp'
                    appName: 'cleansync-api'
                    package: '$(Pipeline.Workspace)/drop/**/*.zip'
                    deploymentMethod: 'auto'
```

---

## Monitoring & Logging

### Application Insights (Azure)

```bash
# Create Application Insights
az monitor app-insights component create --app cleansync-appinsights --location eastus --resource-group cleansync-rg

# Get instrumentation key
az monitor app-insights component show --app cleansync-appinsights --resource-group cleansync-rg --query instrumentationKey
```

Add to `appsettings.json`:
```json
{
  'ApplicationInsights': {
    'ConnectionString': 'InstrumentationKey=your-key;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/'
  }
}
```

### Kubernetes Monitoring (Prometheus + Grafana)

```yaml
# k8s/monitoring.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: cleansync
data:
  prometheus.yml: |
    scrape_configs:
      - job_name: 'cleansync-api'
        kubernetes_sd_configs:
          - role: pod
        relabel_configs:
          - source_labels: [__meta_kubernetes_pod_label_app]
            action: keep
            regex: cleansync-api
          - source_labels: [__meta_kubernetes_pod_container_port_number]
            action: keep
            regex: '5000'
```

---

## Security Checklist

- [ ] Use Key Vault for secrets management
- [ ] Enable HTTPS/TLS
- [ ] Configure firewall rules
- [ ] Use managed identities
- [ ] Enable vulnerability scanning
- [ ] Regular security updates
- [ ] Implement rate limiting
- [ ] Configure audit logging
- [ ] Use network isolation (VNet/subnet)
- [ ] Enable Defender for Cloud

---

## Troubleshooting

### Docker Issues

```bash
# View container logs
docker logs <container-id>

# Inspect container
docker inspect <container-id>

# Check networking
docker network inspect cleansync-network
```

### Kubernetes Issues

```bash
# Describe pod
kubectl describe pod <pod-name> -n cleansync

# Check pod logs
kubectl logs <pod-name> -n cleansync

# Check events
kubectl get events -n cleansync --sort-by='.lastTimestamp'

# Port forward for debugging
kubectl port-forward <pod-name> 5000:5000 -n cleansync
```

### Azure Issues

```bash
# View App Service logs
az webapp log tail --name cleansync-api --resource-group cleansync-rg

# Enable diagnostics
az webapp log config --resource-group cleansync-rg --name cleansync-api --web-server-logging on

# Check deployment status
az webapp deployment list --name cleansync-api --resource-group cleansync-rg
```