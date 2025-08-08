# SimpleSerialToApi 배포 스크립트
# PowerShell 5.1+ 또는 PowerShell Core 7+ 필요

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",
    
    [Parameter(Mandatory=$false)]
    [switch]$SelfContained = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateMsi = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$CreatePortable = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$RunTests = $true,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./dist"
)

# 색상 출력을 위한 함수
function Write-ColorOutput {
    param(
        [string]$Message,
        [ConsoleColor]$ForegroundColor = [ConsoleColor]::White
    )
    
    $originalColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Host $Message
    $Host.UI.RawUI.ForegroundColor = $originalColor
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ $Message" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" -ForegroundColor Red
}

# 배포 시작
Write-Info "SimpleSerialToApi 배포 스크립트 시작"
Write-Info "버전: $Version"
Write-Info "구성: $Configuration"
Write-Info "런타임: $Runtime"
Write-Info "자체 포함: $SelfContained"
Write-Info "출력 경로: $OutputPath"

# 시작 시간 기록
$startTime = Get-Date

try {
    # 1. 사전 조건 확인
    Write-Info "사전 조건 확인 중..."
    
    # .NET SDK 버전 확인
    $dotnetVersion = & dotnet --version
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK를 찾을 수 없습니다. https://dotnet.microsoft.com/download에서 .NET 8 SDK를 설치하세요."
    }
    
    if ([Version]$dotnetVersion -lt [Version]"8.0.0") {
        throw ".NET 8 SDK가 필요합니다. 현재 버전: $dotnetVersion"
    }
    Write-Success ".NET SDK 버전: $dotnetVersion"
    
    # 솔루션 파일 확인
    if (!(Test-Path "SimpleSerialToApi.sln")) {
        throw "SimpleSerialToApi.sln 파일을 찾을 수 없습니다. 올바른 디렉토리에서 실행하세요."
    }
    Write-Success "솔루션 파일 확인"
    
    # WiX Toolset 확인 (MSI 생성 시 필요)
    if ($CreateMsi) {
        $wixPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin"
        if (!(Test-Path "$wixPath\candle.exe")) {
            Write-Warning "WiX Toolset을 찾을 수 없습니다. MSI 생성을 건너뛰겠습니다."
            $CreateMsi = $false
        } else {
            Write-Success "WiX Toolset 확인"
        }
    }
    
    # 출력 디렉토리 생성
    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
    Write-Success "출력 디렉토리 생성: $OutputPath"
    
    # 2. 소스 코드 정리 및 복원
    Write-Info "소스 코드 정리 및 패키지 복원 중..."
    
    # Clean
    & dotnet clean --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "소스 코드 정리 실패"
    }
    Write-Success "소스 코드 정리 완료"
    
    # Restore
    & dotnet restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "패키지 복원 실패"
    }
    Write-Success "패키지 복원 완료"
    
    # 3. 테스트 실행 (선택사항)
    if ($RunTests) {
        Write-Info "단위 테스트 실행 중..."
        
        & dotnet test --configuration $Configuration --no-restore --verbosity minimal --logger trx --results-directory "$OutputPath/TestResults"
        if ($LASTEXITCODE -ne 0) {
            throw "단위 테스트 실패"
        }
        Write-Success "단위 테스트 통과"
    }
    
    # 4. 애플리케이션 빌드
    Write-Info "애플리케이션 빌드 중..."
    
    & dotnet build --configuration $Configuration --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "빌드 실패"
    }
    Write-Success "빌드 완료"
    
    # 5. 애플리케이션 게시
    Write-Info "애플리케이션 게시 중..."
    
    $publishArgs = @(
        "publish",
        "SimpleSerialToApi/SimpleSerialToApi.csproj",
        "--configuration", $Configuration,
        "--runtime", $Runtime,
        "--no-build",
        "--verbosity", "minimal",
        "--output", "$OutputPath/publish"
    )
    
    if ($SelfContained) {
        $publishArgs += "--self-contained", "true"
        Write-Info "자체 포함 배포로 게시"
    } else {
        $publishArgs += "--self-contained", "false"
        Write-Info "Framework 종속 배포로 게시"
    }
    
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "게시 실패"
    }
    Write-Success "애플리케이션 게시 완료"
    
    # 6. 포터블 패키지 생성
    if ($CreatePortable) {
        Write-Info "포터블 패키지 생성 중..."
        
        $portablePath = "$OutputPath/SimpleSerialToApi-Portable-v$Version-$Runtime"
        New-Item -ItemType Directory -Path $portablePath | Out-Null
        
        # 게시된 파일 복사
        Copy-Item "$OutputPath/publish/*" $portablePath -Recurse
        
        # 문서 복사
        $docPath = "$portablePath/Documentation"
        New-Item -ItemType Directory -Path $docPath | Out-Null
        if (Test-Path "Documentation") {
            Copy-Item "Documentation/*" $docPath -Recurse
        }
        
        # 라이선스 및 README 복사
        if (Test-Path "LICENSE") {
            Copy-Item "LICENSE" $portablePath
        }
        if (Test-Path "README.md") {
            Copy-Item "README.md" $portablePath
        }
        
        # 포터블 README 생성
        $portableReadme = @"
# SimpleSerialToApi Portable v$Version

## 실행 방법
1. SimpleSerialToApi.exe를 더블클릭하여 실행
2. 처음 실행 시 Windows Defender SmartScreen 경고가 나타날 수 있습니다
3. "추가 정보" → "실행" 클릭하여 실행

## 시스템 요구사항
- Windows 10 (1809 이상) 또는 Windows 11
- .NET 8 Runtime (자체 포함 배포의 경우 불필요)
- Serial 포트 또는 USB-to-Serial 어댑터

## 설정 파일
- App.config: 애플리케이션 설정
- appsettings.json: 추가 구성 설정

## 문서
- Documentation/ 폴더에서 상세 매뉴얼 확인

## 지원
- 이메일: support@yourcompany.com
- 웹사이트: https://yourcompany.com/support

빌드 날짜: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
빌드 버전: $Version
런타임: $Runtime
자체 포함: $SelfContained
"@
        $portableReadme | Out-File -FilePath "$portablePath/README.txt" -Encoding UTF8
        
        # ZIP 압축
        $zipPath = "$OutputPath/SimpleSerialToApi-Portable-v$Version-$Runtime.zip"
        Compress-Archive -Path "$portablePath/*" -DestinationPath $zipPath -Force
        
        Write-Success "포터블 패키지 생성: $zipPath"
    }
    
    # 7. MSI 설치 프로그램 생성
    if ($CreateMsi) {
        Write-Info "MSI 설치 프로그램 생성 중..."
        
        $wixPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin"
        $wxsFile = "Documentation/Deployment/SimpleSerialToApi.wxs"
        
        if (!(Test-Path $wxsFile)) {
            Write-Warning "WiX 소스 파일을 찾을 수 없습니다: $wxsFile"
            Write-Warning "MSI 생성을 건너뛰겠습니다"
        } else {
            # WiX 변수 설정
            $wixVars = @(
                "-dSimpleSerialToApi.TargetPath=$OutputPath\publish\SimpleSerialToApi.exe",
                "-dSimpleSerialToApi.TargetDir=$OutputPath\publish\",
                "-dVersion=$Version"
            )
            
            # Candle (컴파일)
            & "$wixPath\candle.exe" -out "$OutputPath/" $wxsFile @wixVars
            if ($LASTEXITCODE -ne 0) {
                throw "WiX 컴파일 실패"
            }
            
            # Light (링크)
            & "$wixPath\light.exe" -out "$OutputPath/SimpleSerialToApi-v$Version.msi" "$OutputPath/SimpleSerialToApi.wixobj" -ext WixUIExtension
            if ($LASTEXITCODE -ne 0) {
                throw "WiX 링크 실패"
            }
            
            Write-Success "MSI 설치 프로그램 생성: SimpleSerialToApi-v$Version.msi"
        }
    }
    
    # 8. 체크섬 생성
    Write-Info "체크섬 생성 중..."
    
    $checksumFile = "$OutputPath/checksums.txt"
    $checksumContent = @()
    
    Get-ChildItem $OutputPath -File | ForEach-Object {
        if ($_.Extension -in @(".zip", ".msi", ".exe")) {
            $hash = Get-FileHash $_.FullName -Algorithm SHA256
            $checksumContent += "$($hash.Hash.ToLower())  $($_.Name)"
        }
    }
    
    $checksumContent | Out-File -FilePath $checksumFile -Encoding UTF8
    Write-Success "체크섬 파일 생성: checksums.txt"
    
    # 9. 빌드 정보 파일 생성
    Write-Info "빌드 정보 생성 중..."
    
    $buildInfo = @{
        Version = $Version
        Configuration = $Configuration
        Runtime = $Runtime
        SelfContained = $SelfContained
        BuildDate = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        BuildMachine = $env:COMPUTERNAME
        BuildUser = $env:USERNAME
        DotNetVersion = $dotnetVersion
        GitCommit = if (Get-Command git -ErrorAction SilentlyContinue) { & git rev-parse HEAD } else { "N/A" }
        GitBranch = if (Get-Command git -ErrorAction SilentlyContinue) { & git branch --show-current } else { "N/A" }
    }
    
    $buildInfo | ConvertTo-Json -Depth 2 | Out-File -FilePath "$OutputPath/build-info.json" -Encoding UTF8
    Write-Success "빌드 정보 생성: build-info.json"
    
    # 10. 배포 완료 요약
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Success "배포 완료!"
    Write-Info "소요 시간: $($duration.TotalMinutes.ToString("F1"))분"
    
    Write-Info "생성된 파일들:"
    Get-ChildItem $OutputPath -File | ForEach-Object {
        $size = if ($_.Length -gt 1MB) { 
            "$([math]::Round($_.Length / 1MB, 1)) MB" 
        } elseif ($_.Length -gt 1KB) { 
            "$([math]::Round($_.Length / 1KB, 1)) KB" 
        } else { 
            "$($_.Length) bytes" 
        }
        Write-Info "  $($_.Name) ($size)"
    }
    
    Write-Success "모든 배포 아티팩트가 $OutputPath 디렉토리에 생성되었습니다."
    
} catch {
    Write-Error "배포 실패: $($_.Exception.Message)"
    exit 1
}

# 배포 검증 (선택사항)
Write-Info "배포 검증을 수행하시겠습니까? (y/N)"
$response = Read-Host
if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Info "배포 검증 시작..."
    
    # 실행 파일 검증
    $exePath = "$OutputPath/publish/SimpleSerialToApi.exe"
    if (Test-Path $exePath) {
        try {
            $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
            Write-Success "실행 파일 버전: $version"
        } catch {
            Write-Warning "실행 파일 버전 정보를 읽을 수 없습니다"
        }
    }
    
    # 종속성 확인
    Write-Info "주요 종속성 확인..."
    $requiredFiles = @(
        "SimpleSerialToApi.exe",
        "SimpleSerialToApi.dll",
        "App.config"
    )
    
    foreach ($file in $requiredFiles) {
        if (Test-Path "$OutputPath/publish/$file") {
            Write-Success "$file ✓"
        } else {
            Write-Warning "$file ✗"
        }
    }
    
    Write-Success "배포 검증 완료"
}

Write-Info "배포 스크립트 종료"