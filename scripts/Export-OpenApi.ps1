[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [uri]$ApiBaseUrl,

    [string]$OutputPath = 'docs\api\openapi.yml'
)

$ErrorActionPreference = 'Stop'

if ($ApiBaseUrl.Scheme -notin @('http', 'https')) {
    throw 'ApiBaseUrl must use HTTP or HTTPS.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$outputFile = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
}

$outputDirectory = Split-Path -Parent $outputFile
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$jsonFile = [System.IO.Path]::GetTempFileName()
$specificationUri = '{0}/openapi/v1.json' -f $ApiBaseUrl.AbsoluteUri.TrimEnd('/')

try {
    Invoke-WebRequest -Uri $specificationUri -OutFile $jsonFile -UseBasicParsing

    & npx --yes '@redocly/cli' bundle $jsonFile --output $outputFile --ext yaml
    if ($LASTEXITCODE -ne 0) {
        throw "Redocly CLI failed with exit code $LASTEXITCODE."
    }

    $generatedFile = Get-Item -LiteralPath $outputFile
    if ($generatedFile.Length -eq 0) {
        throw "The generated OpenAPI file is empty: $outputFile"
    }

    Write-Output "OpenAPI YAML written to $outputFile"
}
finally {
    Remove-Item -LiteralPath $jsonFile -Force -ErrorAction SilentlyContinue
}
