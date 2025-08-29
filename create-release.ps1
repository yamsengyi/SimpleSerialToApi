# 릴리스 태그 생성 및 GitHub에 푸시하는 PowerShell 스크립트

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "🚀 릴리스 $Version 생성 중..." -ForegroundColor Green

try {
    # 태그 생성
    git tag -a $Version -m "Release $Version"
    
    # GitHub에 태그 푸시
    git push origin $Version
    
    Write-Host "✅ 릴리스 $Version 생성 완료!" -ForegroundColor Green
    Write-Host "📦 GitHub Actions에서 자동 빌드가 시작됩니다." -ForegroundColor Yellow
    Write-Host "🔗 릴리스 페이지: https://github.com/yamsengyi/SimpleSerialToApi/releases" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ 릴리스 생성 실패: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
