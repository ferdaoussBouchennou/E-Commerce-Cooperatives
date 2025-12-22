# Script pour nettoyer et reconstruire le projet
Write-Host "Nettoyage des dossiers bin et obj..." -ForegroundColor Yellow

# Supprimer les dossiers bin et obj
if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin"
    Write-Host "Dossier bin supprimé" -ForegroundColor Green
}

if (Test-Path "obj") {
    Remove-Item -Recurse -Force "obj"
    Write-Host "Dossier obj supprimé" -ForegroundColor Green
}

Write-Host "`nNettoyage terminé. Veuillez maintenant:" -ForegroundColor Yellow
Write-Host "1. Ouvrir Visual Studio" -ForegroundColor Cyan
Write-Host "2. Menu: Build -> Rebuild Solution" -ForegroundColor Cyan
Write-Host "3. Redémarrer l'application" -ForegroundColor Cyan

