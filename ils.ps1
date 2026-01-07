$ModelName = "eloi" 
$ModelfilePath = ".\eloi_Modelfile" 
$OllamaUIUrl = "http://localhost:11434"
$env:OLLAMA_DEFAULT_MODEL = "eloi"

function Test-OllamaInstalled {
    try {
        $null = & ollama --version 2>$null
        return $true
    } catch {
        return $false
    }
}

function Start-OllamaService {
    Write-Host "Starting Ollama service..."
    try {
        Start-Process "ollama" -ArgumentList "serve" -WindowStyle Hidden
        Start-Sleep -Seconds 3
    } catch {
        Write-Error "Failed to start service. Ensure Ollama is installed."
        exit 1
    }
}

if (-not (Test-OllamaInstalled)) {
    Write-Error "Ollama is not installed. Please install it from https://ollama.com/download"
    exit 1
}

if (-not (Get-Process -Name "ollama" -ErrorAction SilentlyContinue)) {
    Start-OllamaService
} else {
    Write-Host "Ollama service is already running."
}

if (-Not (Test-Path $ModelfilePath)) {
    Write-Error "Modelfile not found at: $ModelfilePath"
    exit 1
}

Write-Host "Building model '$ModelName' from Modelfile..."
& ollama create $ModelName -f $ModelfilePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Model build failed. Check your Modelfile syntax."
    exit 1
}

Write-Host "Model '$ModelName' built successfully."
Start-Process "$env:LOCALAPPDATA\Programs\Ollama\Ollama.exe"
Write-Host "Eloi started. Ollama with '$ModelName'."

