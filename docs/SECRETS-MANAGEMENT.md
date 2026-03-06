# Secrets Management - Guía Completa

Esta guía explica cómo configurar y usar **secrets management** en lugar de hardcodear secrets en archivos de configuración.

##  ¿Por qué NO usar appsettings.json para secrets?

**Problemas de hardcodear secrets**:
-  Secrets en repositorio Git (riesgo de seguridad)
-  Difícil rotar secrets
-  No hay auditoría de accesos
-  Secrets en logs y crash dumps
-  Compartir secrets entre equipos inseguro
-  No cumple con compliance (SOC2, ISO 27001)

**Solución: External Secrets Management**:
-  Secrets centralizados en un vault
-  Rotación automática de secrets
-  Auditoría completa de accesos
-  Encriptación en reposo y en tránsito
-  Control de acceso granular
-  Cumplimiento con estándares de seguridad

---

##  Providers Soportados

### 1. **Infisical** (Recomendado para startups)

**Ventajas**:
- Open source y self-hosted
- UI moderna e intuitiva
- Fácil de configurar
- Gratis para equipos pequeños
- Soporte para múltiples environments (dev, staging, prod)

**Casos de uso**:
- Startups y equipos pequeños
- Proyectos con presupuesto limitado
- Necesitas self-hosted

### 2. **HashiCorp Vault** (Enterprise-grade)

**Ventajas**:
- Muy maduro y battle-tested
- Features avanzadas (dynamic secrets, encryption as a service)
- Soporte enterprise
- Integración con todo

**Casos de uso**:
- Empresas grandes
- Requisitos de compliance estrictos
- Infraestructura compleja

### 3. **Azure Key Vault** (Para Azure)

**Ventajas**:
- Integración nativa con Azure
- Managed service (sin mantenimiento)
- Managed Identity (no credentials)

**Casos de uso**:
- Aplicaciones en Azure
- Usas Azure Active Directory
- Prefieres managed services

### 4. **AWS Secrets Manager** (Para AWS)

**Ventajas**:
- Integración nativa con AWS
- Rotación automática
- IAM Roles (no credentials)

**Casos de uso**:
- Aplicaciones en AWS/EKS
- Infraestructura AWS-first

---

##  Comparación Rápida

| Feature | Infisical | Vault | Azure KV | AWS SM |
|---------|-----------|-------|----------|---------|
| **Precio** | Gratis/Paid | Gratis/Enterprise | Pay per use | Pay per use |
| **Self-hosted** |  |  |  |  |
| **UI** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Facilidad** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Features** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Community** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |

---

## ️ Implementación

### Opción 1: Desarrollo Local con Docker Compose

#### Levantar Infisical

```bash
# Levantar Infisical + Vault
docker-compose -f docker-compose.secrets.yml up -d infisical vault

# Inicializar Vault con secrets
docker-compose -f docker-compose.secrets.yml --profile init up vault-init

# Acceder a Infisical UI
open http://localhost:8080
```

#### Configurar aplicación

```bash
# Usar Vault
export SECRETS_PROVIDER=vault
export VAULT_ADDR=http://localhost:8200
export VAULT_TOKEN=root

# O usar Infisical
export SECRETS_PROVIDER=infisical
export INFISICAL_CLIENT_ID=your-client-id
export INFISICAL_CLIENT_SECRET=your-client-secret

# Levantar aplicación
docker-compose -f docker-compose.secrets.yml --profile clean up -d
```

---

### Opción 2: Kubernetes con External Secrets Operator

#### 1. Instalar External Secrets Operator

```bash
helm repo add external-secrets https://charts.external-secrets.io
helm install external-secrets external-secrets/external-secrets \
  -n external-secrets-system --create-namespace
```

#### 2. Configurar SecretStore (elige uno)

**Para Infisical**:
```bash
kubectl apply -f src/CleanArchitecture/kubernetes/external-secrets/secret-store-infisical.yaml
```

**Para Vault**:
```bash
kubectl apply -f src/CleanArchitecture/kubernetes/external-secrets/secret-store-vault.yaml
```

**Para Azure Key Vault**:
```bash
kubectl apply -f src/CleanArchitecture/kubernetes/external-secrets/secret-store-azure.yaml
```

#### 3. Crear ExternalSecret

```bash
kubectl apply -f src/CleanArchitecture/kubernetes/external-secrets/external-secret.yaml
```

#### 4. Verificar

```bash
# Ver secret generado
kubectl get secret cleanarchitecture-secrets -o yaml

# Ver status
kubectl describe externalsecret cleanarchitecture-secrets
```

---

### Opción 3: Código .NET Direct Integration

#### Program.cs con Infisical

```csharp
var builder = WebApplication.CreateBuilder(args);

// Cargar secrets desde Infisical
builder.Configuration.AddInfisical(options =>
{
    options.ServerUrl = Environment.GetEnvironmentVariable("INFISICAL_URL")
        ?? "https://app.infisical.com";
    options.ClientId = Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID")!;
    options.ClientSecret = Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET")!;
    options.WorkspaceId = Environment.GetEnvironmentVariable("INFISICAL_WORKSPACE_ID")!;
    options.Environment = Environment.GetEnvironmentVariable("INFISICAL_ENVIRONMENT")
        ?? "production";
}, optional: false);

// Ahora los secrets están disponibles
var jwtSecret = builder.Configuration["Jwt:SecretKey"];
```

#### Program.cs con HashiCorp Vault

```csharp
builder.Configuration.AddHashiCorpVault(options =>
{
    options.Address = Environment.GetEnvironmentVariable("VAULT_ADDR")!;
    options.Token = Environment.GetEnvironmentVariable("VAULT_TOKEN")!;
    options.SecretPath = "application";
    options.MountPoint = "secret";
}, optional: false);
```

#### Program.cs con Azure Key Vault

```csharp
var keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL");
builder.Configuration.AddAzureKeyVault(keyVaultUrl!, optional: false);
```

---

##  Ejemplo: Migrar de appsettings.json a Vault

### Antes (inseguro):

**appsettings.json**:
```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKey123!",  //  En Git!
    "Issuer": "MyAPI"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Password=P@ssw0rd"  //  Contraseña visible!
  }
}
```

### Después (seguro):

**1. Guardar en Vault**:
```bash
vault kv put secret/application/jwt secret-key="YourSuperSecretKey123!"
vault kv put secret/application/database connection-string="Server=...;Password=P@ssw0rd"
```

**2. appsettings.json (sin secrets)**:
```json
{
  "Jwt": {
    "Issuer": "MyAPI",  //  Solo config pública
    "ExpiryMinutes": 60
  }
}
```

**3. Program.cs**:
```csharp
builder.Configuration.AddHashiCorpVault(options => {
    options.Address = Environment.GetEnvironmentVariable("VAULT_ADDR")!;
    options.Token = Environment.GetEnvironmentVariable("VAULT_TOKEN")!;
    options.SecretPath = "application";
});

// Los secrets se cargan automáticamente y sobrescriben appsettings.json
```

**4. Variables de entorno** (solo las URLs, no los secrets):
```bash
export VAULT_ADDR=http://vault:8200
export VAULT_TOKEN=root  # En prod esto viene del Kubernetes Service Account
```

---

##  Rotación de Secrets

### Rotación Manual

```bash
# 1. Actualizar secret en Vault
vault kv put secret/application/jwt secret-key="NewSecretKey456!"

# 2. External Secrets Operator lo sincroniza automáticamente (1h default)

# 3. Forzar sincronización inmediata
kubectl annotate externalsecret cleanarchitecture-secrets \
  force-sync=$(date +%s) --overwrite

# 4. Reiniciar pods para cargar nuevo secret
kubectl rollout restart deployment cleanarchitecture-api
```

### Rotación Automática

**En Kubernetes**:
```yaml
spec:
  refreshInterval: 15m  # Sincronizar cada 15 minutos
```

**En .NET** (Hot reload):
```csharp
builder.Configuration.AddInfisical(options => { ... }, optional: false);

// Opcional: Recargar configuración periódicamente
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddHostedService<ConfigurationRefreshService>();
```

---

##  Auditoría y Monitoring

### Ver quién accedió a secrets

**Vault**:
```bash
vault audit enable file file_path=/vault/logs/audit.log
vault audit list
```

**Infisical**:
- Dashboard → Audit Logs
- Ver quién, cuándo y qué secrets fueron accedidos

**Azure Key Vault**:
```bash
az monitor activity-log list --resource-id /subscriptions/.../keyVault/your-vault
```

---

##  Best Practices

###  DO

1. **Usar secrets management desde día 1**
2. **Rotar secrets regularmente** (cada 90 días mínimo)
3. **Limitar acceso** con RBAC/IAM
4. **Habilitar auditoría** en el vault
5. **Usar diferentes secrets por environment** (dev, staging, prod)
6. **Encriptar secrets en tránsito** (HTTPS/TLS)
7. **No loggear secrets** nunca
8. **Usar Managed Identity** en cloud (Azure/AWS)

###  DON'T

1. **NO commits de secrets en Git** (usar .gitignore)
2. **NO hardcodear secrets** en código
3. **NO compartir secrets** por Slack/Email
4. **NO usar mismos secrets** en dev y prod
5. **NO dar acceso de vault** a todos
6. **NO usar secrets en URLs** (query parameters)
7. **NO ignorar alerts** de secrets expuestos
8. **NO usar "admin" o "root"** passwords default

---

##  Testing

### Unit Tests (usar mocks)

```csharp
var configMock = new Mock<IConfiguration>();
configMock.Setup(c => c["Jwt:SecretKey"]).Returns("test-secret");

var service = new JwtService(configMock.Object);
```

### Integration Tests (In-Memory Vault)

```csharp
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    ["Jwt:SecretKey"] = "integration-test-secret"
});
```

---

##  Referencias

- [Infisical Documentation](https://infisical.com/docs)
- [HashiCorp Vault Documentation](https://www.vaultproject.io/docs)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/azure/key-vault/general/best-practices)
- [External Secrets Operator](https://external-secrets.io/)
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

---

## 🆘 Troubleshooting

### Error: "Failed to authenticate with Infisical"

```bash
# Verificar credentials
curl -X POST https://app.infisical.com/api/v1/auth/universal-auth/login \
  -H "Content-Type: application/json" \
  -d '{"clientId":"xxx","clientSecret":"yyy"}'
```

### Error: "Vault sealed"

```bash
# Verificar status
vault status

# Unseal (dev mode no necesita)
vault operator unseal
```

### Error: "Access denied to Key Vault"

```bash
# Verificar Service Principal tiene permisos
az keyvault set-policy --name your-vault \
  --spn YOUR_CLIENT_ID \
  --secret-permissions get list
```

---

**¿Preguntas?** Revisa los ejemplos en:
- `src/CleanArchitecture/API/Program.WithSecrets.cs`
- `src/CleanArchitecture/kubernetes/external-secrets/`
- `docker-compose.secrets.yml`
