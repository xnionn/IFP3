param([string]$InputDocx, [string]$OutputPdf)
$ErrorActionPreference = 'Stop'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
$document = $null
try {
    $document = $word.Documents.Open($InputDocx, $false, $true)
    $document.Repaginate()
    $document.ExportAsFixedFormat($OutputPdf, 17)
    Write-Output ('Pages: ' + $document.ComputeStatistics(2))
}
finally {
    if ($null -ne $document) {
        $document.Close(0)
        [void][Runtime.InteropServices.Marshal]::ReleaseComObject($document)
    }
    $word.Quit()
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($word)
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
