# ============================================================
# MariCamiStore — Azure App Service Deploy (ZIP Deploy)
# Requisito: az CLI instalado y logueado (az login)
# ============================================================

$SUBSCRIPTION_ID    = "f8927ede-2d3f-4156-b1b1-fd45bb66694f"
$RESOURCE_GROUP     = "Testing_Produccion"
$WEB_APP_NAME       = "maricamistoreweb"
$CONNECTION_STRING  = "Server=tcp:sigam.database.windows.net,1433;Initial Catalog=maricamistore;Persist Security Info=False;User ID=sigam;Password=kdf38K)0;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$DEFAULT_ORG_ID     = "64B4D953-66D5-409E-929D-6036111FB710"

$PROJECT_PATH       = "MariCamiStore/MariCamiStore.csproj"
$PUBLISH_DIR        = "./publish"
$ZIP_PATH           = "./publish.zip"

Write-Host "==> 0. Seleccionando suscripcion..." -ForegroundColor Cyan
az account set --subscription $SUBSCRIPTION_ID

Write-Host "==> 1. Configurando variables de entorno..." -ForegroundColor Cyan
az webapp config appsettings set `
  --name $WEB_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --settings `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "ConnectionString=$CONNECTION_STRING" `
    "DefaultOrganizationId=$DEFAULT_ORG_ID"

Write-Host "==> 2. Habilitando logs de App Service..." -ForegroundColor Cyan
az webapp log config `
  --name $WEB_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --application-logging filesystem `
  --level information

Write-Host "==> 3. Publicando app..." -ForegroundColor Cyan
if (Test-Path $PUBLISH_DIR) { Remove-Item $PUBLISH_DIR -Recurse -Force }
if (Test-Path $ZIP_PATH)    { Remove-Item $ZIP_PATH -Force }

dotnet publish $PROJECT_PATH -c Release -o $PUBLISH_DIR

Write-Host "==> 4. Comprimiendo..." -ForegroundColor Cyan
Compress-Archive -Path "$PUBLISH_DIR/*" -DestinationPath $ZIP_PATH -Force

Write-Host "==> 5. Desplegando a Azure..." -ForegroundColor Cyan
az webapp deploy `
  --resource-group $RESOURCE_GROUP `
  --name $WEB_APP_NAME `
  --src-path $ZIP_PATH `
  --type zip

Write-Host ""
Write-Host "Deploy completado." -ForegroundColor Green
Write-Host "URL: https://$WEB_APP_NAME.azurewebsites.net" -ForegroundColor Green
