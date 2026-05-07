# batch_interpret.ps1
# Not: Yorumların TÜRKÇE, TARAFSIZ, KISA ve NET olması için backend promptu güncellenmiştir.
# Script backend üzerinden LM Studio'yu tetikler, DB'ye yazdırır ve ayrıca QuranData altında dosya bazlı persist eder.

$backendUrl = $null
$lmStudioUrl = "http://127.0.0.1:1234/v1"

# Token harcamamak için varsayılan olarak sadece 1 ayet işler. İsterseniz 0 yaparak tüm ayetleri işletebilirsiniz.
$maxVersesToProcess = 10

function Get-RepoRoot {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        return (Get-Location).Path
    }
    return (Split-Path -Parent $scriptPath)
}

function To-FileSafeSlug {
    param([string]$value)
    if ([string]::IsNullOrWhiteSpace($value)) { return "unknown" }
    $slug = $value.ToLowerInvariant()
    $slug = $slug -replace '[^a-z0-9\-_.]+', '-'
    $slug = $slug.Trim('-')
    if ([string]::IsNullOrWhiteSpace($slug)) { return "unknown" }
    return $slug
}

function Get-LMStudioModel {
    try {
        $data = Invoke-RestMethod -Uri "$lmStudioUrl/models" -ErrorAction Stop
        if ($null -eq $data -or $null -eq $data.data -or $data.data.Count -lt 1) {
            Write-Error "LM Studio /models boş döndü."
            return $null
        }
        return $data.data[0].id
    } catch {
        Write-Error "LM Studio'ya bağlanılamadı veya model yüklü değil: $($_.Exception.Message)"
        return $null
    }
}

function Resolve-BackendUrl {
    param([string[]]$candidates)

    $envUrl = $env:KURAN_BACKEND_URL
    if ([string]::IsNullOrWhiteSpace($envUrl)) { $envUrl = $env:BACKEND_URL }
    if (-not [string]::IsNullOrWhiteSpace($envUrl)) {
        $candidates = @($envUrl) + $candidates
    }

    foreach ($base in $candidates) {
        if ([string]::IsNullOrWhiteSpace($base)) { continue }
        $trimmed = $base.TrimEnd('/')
        try {
            $req = Invoke-WebRequest -Uri "$trimmed/Surahs" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($null -ne $req -and $req.StatusCode -ge 200 -and $req.StatusCode -lt 500) {
                return $trimmed
            }
        } catch {
        }

        try {
            $req2 = Invoke-WebRequest -Uri "$trimmed/swagger/index.html" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($null -ne $req2 -and $req2.StatusCode -ge 200 -and $req2.StatusCode -lt 500) {
                return $trimmed
            }
        } catch {
        }
    }

    return $null
}

function Ensure-BackendModel {
    param([string]$modelName)

    $providerBody = @{
        providerName = "LMStudio"
        baseUrl      = $lmStudioUrl
    } | ConvertTo-Json

    try {
        $providerResponse = Invoke-RestMethod -Method Post -Uri "$backendUrl/LLM/providers" -Body $providerBody -ContentType "application/json" -ErrorAction Stop
        $providerId = $providerResponse.providerId
        if (-not $providerId) { throw "Backend providerId dönmedi." }
    } catch {
        throw "Backend'e bağlanılamadı (/LLM/providers). Backend çalışıyor mu? ($backendUrl) Detay: $($_.Exception.Message)"
    }

    $modelBody = @{
        providerId  = $providerId
        modelName   = $modelName
        displayName = "$modelName (Local)"
        isFree      = $true
    } | ConvertTo-Json

    try {
        $modelResponse = Invoke-RestMethod -Method Post -Uri "$backendUrl/LLM/models" -Body $modelBody -ContentType "application/json" -ErrorAction Stop
        $modelId = $modelResponse.modelId
        if (-not $modelId) { throw "Backend modelId dönmedi." }
        return $modelId
    } catch {
        throw "Backend'e bağlanılamadı (/LLM/models). Backend çalışıyor mu? ($backendUrl) Detay: $($_.Exception.Message)"
    }
}

function Get-InterpretationFilePath {
    param(
        [string]$repoRoot,
        [string]$modelName,
        [int]$surahNumber,
        [int]$verseNumber
    )

    $modelSlug = To-FileSafeSlug -value $modelName
    $quranDataRoot = Join-Path $repoRoot "KuranApp\Data\QuranData"
    $interpretationsRoot = Join-Path $quranDataRoot "Interpretations"
    $modelDir = Join-Path $interpretationsRoot $modelSlug
    $baseDir = Join-Path $modelDir ("surah_{0}" -f $surahNumber)
    if (-not (Test-Path $baseDir)) {
        New-Item -ItemType Directory -Path $baseDir -Force | Out-Null
    }

    return (Join-Path $baseDir ("verse_{0}.json" -f $verseNumber))
}

function Assert-SafeFilePath {
    param(
        [string]$filePath,
        [string]$repoRoot
    )

    $fullPath = [System.IO.Path]::GetFullPath($filePath)
    $expectedRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "KuranApp\Data\QuranData"))

    $driveRoot = [System.IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.TrimEnd('\') -ieq $driveRoot.TrimEnd('\')) {
        throw "Hedef dosya yolu sürücü kökü olamaz: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath -PathType Container) {
        throw "Hedef dosya yolu klasör olamaz: $fullPath"
    }

    if (-not $fullPath.StartsWith($expectedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Hedef dosya yolu QuranData dışında: $fullPath"
    }

    if ([System.IO.Path]::GetExtension($fullPath) -ne ".json") {
        throw "Hedef dosya uzantısı .json olmalı: $fullPath"
    }
}

function Save-InterpretationToFile {
    param(
        [string]$filePath,
        [string]$repoRoot,
        [string]$modelName,
        [int]$verseId,
        [int]$surahNumber,
        [int]$verseNumber,
        [string]$arabicText,
        [string]$turkishTranslation,
        [string]$interpretationText
    )

    Assert-SafeFilePath -filePath $filePath -repoRoot $repoRoot

    $payload = [ordered]@{
        modelName          = $modelName
        generatedAtUtc     = (Get-Date).ToUniversalTime().ToString('o')
        verseId            = $verseId
        surahNumber        = $surahNumber
        verseNumber        = $verseNumber
        arabicText         = $arabicText
        turkishTranslation = $turkishTranslation
        interpretationTr   = $interpretationText
    }

    ($payload | ConvertTo-Json -Depth 8) | Set-Content -Path $filePath -Encoding UTF8
}

function Process-Interpretations {
    param(
        [int]$modelId,
        [string]$modelName,
        [string]$repoRoot
    )

    Write-Host "Model: $modelName"
    Write-Host "Backend Model ID: $modelId"

    try {
        $verses = Invoke-RestMethod -Method Get -Uri "$backendUrl/Verses" -ErrorAction Stop
    } catch {
        throw "Backend'e bağlanılamadı (/Verses). Backend çalışıyor mu? ($backendUrl) Detay: $($_.Exception.Message)"
    }
    $total = $verses.Count
    Write-Host "Toplam $total ayet bulundu."

    $processed = 0
    $attempted = 0
    foreach ($v in $verses) {
        if ($maxVersesToProcess -gt 0 -and $attempted -ge $maxVersesToProcess) {
            break
        }

        $verseId = [int]$v.id
        $surahNumber = [int]$v.surahId
        $verseNumber = [int]$v.verseNumber

        $filePath = Get-InterpretationFilePath -repoRoot $repoRoot -modelName $modelName -surahNumber $surahNumber -verseNumber $verseNumber

        # İdempotent: dosya varsa bu model için bu ayet yorumlanmış kabul edilir ve atlanır
        if (Test-Path $filePath) {
            Write-Host ("[SKIP] Zaten var: S{0} A{1} ({2})" -f $surahNumber, $verseNumber, $filePath)
            continue
        }

        $attempted++
        Write-Host ("[RUN ] S{0} A{1} (VerseId: {2}) -> {3}" -f $surahNumber, $verseNumber, $verseId, $filePath)

        try {
            # Backend endpointi hem DB'ye yazar, hem de bizim ayrıca dosyaya yazmamız için metni döner
            $gen = Invoke-RestMethod -Method Post -Uri "$backendUrl/Verses/$verseId/interpretations/$modelId/generate" -ErrorAction Stop

            if ($null -eq $gen -or [string]::IsNullOrWhiteSpace($gen.interpretation)) {
                Write-Warning "Yorum üretilemedi (boş yanıt)."
                continue
            }

            $saveParams = @{
                filePath           = $filePath
                repoRoot           = $repoRoot
                modelName          = $modelName
                verseId            = $verseId
                surahNumber        = $surahNumber
                verseNumber        = $verseNumber
                arabicText         = $v.arabicText
                turkishTranslation = $v.turkishTranslation
                interpretationText = $gen.interpretation
            }
            Save-InterpretationToFile @saveParams

            Write-Host ("[OK  ] Kaydedildi: {0}" -f $filePath)
            $processed++

            Start-Sleep -Milliseconds 250
        } catch {
            $err = $_.Exception.Message
            Write-Warning ("[ERR ] S{0} A{1} -> {2}" -f $surahNumber, $verseNumber, $err)
            Start-Sleep -Seconds 2
        }
    }

    Write-Host "Tamamlandı. Yeni yazılan yorum sayısı: $processed"
}

# MAIN
$repoRoot = Get-RepoRoot
Write-Host "Repo root: $repoRoot"

if (-not $backendUrl) {
    $backendUrl = Resolve-BackendUrl -candidates @(
        "http://127.0.0.1:5286",
        "http://localhost:5286",
        "http://127.0.0.1:5000",
        "http://localhost:5000",
        "http://127.0.0.1:5086",
        "http://localhost:5086",
        "https://localhost:7286",
        "https://127.0.0.1:7286"
    )
}
if (-not $backendUrl) {
    Write-Error "Backend'e bağlanılamadı. Önce backend'i çalıştırın ya da KURAN_BACKEND_URL/BACKEND_URL ile adresi verin. Örn: `$env:KURAN_BACKEND_URL='http://127.0.0.1:5000'"
    exit 1
}
Write-Host "Backend: $backendUrl"

Write-Host "LM Studio kontrol ediliyor: $lmStudioUrl"
$currentModel = Get-LMStudioModel
if (-not $currentModel) {
    Write-Error "LM Studio'da yüklü model bulunamadı."
    exit 1
}

try {
    $modelId = Ensure-BackendModel -modelName $currentModel
    Process-Interpretations -modelId $modelId -modelName $currentModel -repoRoot $repoRoot
} catch {
    Write-Error $_
    exit 1
}
