# Kur'an-ı Kerim Veri İndirme ve Ayrıştırma Scripti

$DataDir = "KuranApp/Data/QuranData"
if (!(Test-Path $DataDir)) { New-Item -ItemType Directory -Path $DataDir }

# Nüzul Sırası
$RevelationOrder = @(96, 68, 73, 74, 1, 111, 81, 87, 92, 89, 93, 94, 103, 100, 108, 102, 107, 109, 105, 113, 114, 112, 53, 80, 97, 91, 85, 95, 106, 101, 75, 104, 77, 50, 90, 86, 54, 38, 7, 72, 36, 25, 35, 19, 20, 56, 26, 27, 28, 17, 10, 11, 12, 15, 6, 37, 31, 34, 39, 40, 41, 42, 43, 44, 45, 46, 51, 88, 18, 16, 71, 14, 21, 23, 32, 52, 67, 69, 70, 78, 79, 82, 84, 30, 29, 83, 2, 8, 3, 33, 60, 4, 99, 57, 47, 13, 55, 76, 65, 98, 59, 24, 22, 63, 58, 49, 66, 64, 61, 62, 48, 5, 9, 110)

Write-Host "Arapça metinler indiriliyor..." -ForegroundColor Cyan
$ArabicData = Invoke-RestMethod -Uri "http://api.alquran.cloud/v1/quran/quran-uthmani"

Write-Host "Türkçe (Diyanet) mealleri indiriliyor..." -ForegroundColor Cyan
$TurkishData = Invoke-RestMethod -Uri "http://api.alquran.cloud/v1/quran/tr.diyanet"

$GlobalRevelationIndex = 1

# Nüzul sırasına göre sureleri işle
foreach ($SurahNumber in $RevelationOrder) {
    $SurahArabic = $ArabicData.data.surahs | Where-Object { $_.number -eq $SurahNumber }
    $SurahTurkish = $TurkishData.data.surahs | Where-Object { $_.number -eq $SurahNumber }
    
    $Verses = @()
    for ($i = 0; $i -lt $SurahArabic.ayahs.Count; $i++) {
        $Verses += @{
            Number = $SurahArabic.ayahs[$i].numberInSurah
            RevelationOrder = $GlobalRevelationIndex++
            ArabicText = $SurahArabic.ayahs[$i].text
            TurkishTranslation = $SurahTurkish.ayahs[$i].text
        }
    }
    
    $SurahJson = @{
        SurahNumber = $SurahNumber
        SurahName = $SurahArabic.name
        SurahEnglishName = $SurahArabic.englishName
        RevelationOrder = ($RevelationOrder.IndexOf($SurahNumber) + 1)
        Verses = $Verses
    }
    
    $FilePath = Join-Path $DataDir "surah_$SurahNumber.json"
    $SurahJson | ConvertTo-Json -Depth 10 | Out-File -FilePath $FilePath -Encoding utf8
    Write-Host "Sure $SurahNumber ($($SurahArabic.englishName)) kaydedildi. Nüzul Sırası: $($SurahJson.RevelationOrder)" -ForegroundColor Green
}

Write-Host "Tüm Kur'an verileri başarıyla hazırlandı." -ForegroundColor Cyan
