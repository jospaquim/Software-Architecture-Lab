# External Secrets Operator - Secrets Management para Kubernetes

External Secrets Operator (ESO) sincroniza secrets desde proveedores externos (Infisical, Vault, Azure Key Vault, AWS Secrets Manager, etc.) hacia Kubernetes Secrets.

## Instalación de External Secrets Operator

### 1. Instalar con Helm

```bash
helm repo add external-secrets https://charts.external-secrets.io
helm repo update

helm install external-secrets \
  external-secrets/external-secrets \
  -n external-secrets-system \
  --create-namespace
```

### 2. Verificar instalación

```bash
kubectl get pods -n external-secrets-system
```

## Configuración por Provider

### Opción 1: Infisical

**1. Obtener credentials de Infisical**:
- Ir a https://app.infisical.com
- Crear un Service Token o Universal Auth credentials
- Obtener Workspace ID

**2. Crear SecretStore**:
```bash
# Editar con tus credenciales
kubectl apply -f secret-store-infisical.yaml
```

**3. Crear ExternalSecret**:
```bash
kubectl apply -f external-secret.yaml
```

**4. Verificar**:
```bash
# Ver el SecretStore
kubectl get secretstore

# Ver el ExternalSecret
kubectl get externalsecret

# Ver el Secret generado
kubectl get secret cleanarchitecture-secrets -o yaml
```

---

### Opción 2: HashiCorp Vault

**1. Instalar Vault en Kubernetes** (opcional):
```bash
helm repo add hashicorp https://helm.releases.hashicorp.com
helm install vault hashicorp/vault \
  --set "server.dev.enabled=true" \
  -n vault-system \
  --create-namespace
```

**2. Configurar Kubernetes auth en Vault**:
```bash
# Exec into Vault pod
kubectl exec -it vault-0 -n vault-system -- /bin/sh

# Habilitar Kubernetes auth
vault auth enable kubernetes

# Configurar
vault write auth/kubernetes/config \
    kubernetes_host="https://$KUBERNETES_PORT_443_TCP_ADDR:443"

# Crear policy
vault policy write cleanarchitecture - <<EOF
path "secret/data/application/*" {
  capabilities = ["read"]
}
EOF

# Crear role
vault write auth/kubernetes/role/cleanarchitecture-role \
    bound_service_account_names=cleanarchitecture-sa \
    bound_service_account_namespaces=default \
    policies=cleanarchitecture \
    ttl=24h
```

**3. Guardar secrets en Vault**:
```bash
vault kv put secret/application/jwt secret-key="your-jwt-secret-here"
vault kv put secret/application/database connection-string="Server=..."
vault kv put secret/application/redis connection-string="redis:6379"
```

**4. Aplicar configuración**:
```bash
kubectl apply -f secret-store-vault.yaml
kubectl apply -f external-secret.yaml
```

---

### Opción 3: Azure Key Vault

**1. Crear Key Vault en Azure**:
```bash
az keyvault create \
  --name your-keyvault-name \
  --resource-group your-rg \
  --location eastus
```

**2. Crear Service Principal**:
```bash
az ad sp create-for-rbac -n "cleanarchitecture-sp"

# Output:
# {
#   "appId": "CLIENT_ID",
#   "password": "CLIENT_SECRET",
#   "tenant": "TENANT_ID"
# }
```

**3. Dar permisos al Service Principal**:
```bash
az keyvault set-policy \
  --name your-keyvault-name \
  --spn CLIENT_ID \
  --secret-permissions get list
```

**4. Guardar secrets en Key Vault**:
```bash
az keyvault secret set --vault-name your-keyvault-name \
  --name jwt-secret-key --value "your-jwt-secret"

az keyvault secret set --vault-name your-keyvault-name \
  --name connection-string --value "Server=..."
```

**5. Aplicar configuración**:
```bash
# Editar secret-store-azure.yaml con tus valores
kubectl apply -f secret-store-azure.yaml
kubectl apply -f external-secret.yaml
```

---

## Uso en Deployment

Una vez configurado External Secrets Operator, el Secret `cleanarchitecture-secrets` se crea automáticamente.

Usar en deployment.yaml:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cleanarchitecture-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: cleanarchitecture-api:latest
        env:
          # Cargar desde el secret generado por ESO
          - name: Jwt__SecretKey
            valueFrom:
              secretKeyRef:
                name: cleanarchitecture-secrets
                key: jwt-secret-key

          - name: ConnectionStrings__DefaultConnection
            valueFrom:
              secretKeyRef:
                name: cleanarchitecture-secrets
                key: connection-string

          - name: ConnectionStrings__Redis
            valueFrom:
              secretKeyRef:
                name: cleanarchitecture-secrets
                key: redis-connection-string
```

## Ventajas de External Secrets Operator

 **Secrets centralizados**: Una sola fuente de verdad para secrets
 **Sincronización automática**: Los secrets se actualizan automáticamente
 **Multi-provider**: Soporta 20+ providers (Vault, AWS, Azure, GCP, Infisical, etc.)
 **GitOps friendly**: Los manifests no contienen secrets reales
 **Rotación automática**: Refresh interval configurable
 **Auditoría**: Todos los accesos se registran en el vault
 **Separation of concerns**: Devs no necesitan acceso directo al vault

## Troubleshooting

### Ver logs de External Secrets Operator

```bash
kubectl logs -n external-secrets-system \
  -l app.kubernetes.io/name=external-secrets
```

### Ver estado del ExternalSecret

```bash
kubectl describe externalsecret cleanarchitecture-secrets
```

### Forzar sincronización

```bash
kubectl annotate externalsecret cleanarchitecture-secrets \
  force-sync=$(date +%s) --overwrite
```

### Verificar Secret generado

```bash
kubectl get secret cleanarchitecture-secrets -o jsonpath='{.data}' | jq
```

## Comparación de Providers

| Provider | Pros | Cons | Mejor para |
|----------|------|------|------------|
| **Infisical** | Fácil de usar, UI intuitiva, gratis | Relativamente nuevo | Startups, equipos pequeños |
| **HashiCorp Vault** | Muy maduro, features avanzadas | Complejo de configurar | Empresas grandes |
| **Azure Key Vault** | Integración nativa con Azure | Solo para Azure | Apps en Azure |
| **AWS Secrets Manager** | Integración nativa con AWS | Solo para AWS | Apps en AWS |

## Referencias

- [External Secrets Operator](https://external-secrets.io/)
- [Infisical](https://infisical.com/)
- [HashiCorp Vault](https://www.vaultproject.io/)
- [Azure Key Vault](https://azure.microsoft.com/services/key-vault/)
