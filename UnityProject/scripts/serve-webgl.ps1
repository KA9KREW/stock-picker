param(
  [string]$BuildDir = "./Build/WebGL",
  [int]$Port = 8080
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $BuildDir)) {
  Write-Error "Build directory not found: $BuildDir"
}
$root = Resolve-Path -LiteralPath $BuildDir
$rootPath = $root.ProviderPath

Write-Host "Serving WebGL from $rootPath"
Write-Host "Open http://127.0.0.1:$Port/ (or http://localhost:$Port/)"
Write-Host "Tip: In Unity use File > Build Settings > WebGL > Build And Run for an auto-hosted build."
Write-Host "Press Ctrl+C to stop.`n"

function Start-PythonStaticServer {
  $python = Get-Command python -ErrorAction SilentlyContinue
  if ($python) {
    & python -m http.server $Port --directory $rootPath
    return $true
  }
  $py = Get-Command py -ErrorAction SilentlyContinue
  if ($py) {
    & py -3 -m http.server $Port --directory $rootPath
    return $true
  }
  return $false
}

function Get-MimeType([string]$ext) {
  switch ($ext.ToLowerInvariant()) {
    ".html" { return "text/html; charset=utf-8" }
    ".htm" { return "text/html; charset=utf-8" }
    ".js" { return "application/javascript; charset=utf-8" }
    ".mjs" { return "application/javascript; charset=utf-8" }
    ".wasm" { return "application/wasm" }
    ".data" { return "application/octet-stream" }
    ".json" { return "application/json; charset=utf-8" }
    ".png" { return "image/png" }
    ".jpg" { return "image/jpeg" }
    ".jpeg" { return "image/jpeg" }
    ".svg" { return "image/svg+xml; charset=utf-8" }
    ".css" { return "text/css; charset=utf-8" }
    ".ico" { return "image/x-icon" }
    ".unityweb" { return "application/octet-stream" }
    default { return "application/octet-stream" }
  }
}

function Start-PowerShellStaticServer {
  $listener = [System.Net.HttpListener]::new()
  $prefix = "http://127.0.0.1:$Port/"
  $listener.Prefixes.Add($prefix)
  $listener.Start()
  Write-Host "Using PowerShell HttpListener at $prefix"

  $rootFull = [System.IO.Path]::GetFullPath($rootPath)
  try {
    while ($listener.IsListening) {
      $ctx = $listener.GetContext()
      $req = $ctx.Request
      $res = $ctx.Response
      try {
        $rel = [Uri]::UnescapeDataString($req.Url.AbsolutePath.TrimStart('/'))
        if ([string]::IsNullOrEmpty($rel)) { $rel = "index.html" }
        $candidate = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootFull, $rel.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
        if (-not $candidate.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
          $res.StatusCode = 403
          continue
        }
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
          $res.StatusCode = 404
          continue
        }
        $bytes = [System.IO.File]::ReadAllBytes($candidate)
        $res.StatusCode = 200
        $res.ContentType = Get-MimeType ([System.IO.Path]::GetExtension($candidate))
        $res.ContentLength64 = $bytes.LongLength
        $res.OutputStream.Write($bytes, 0, $bytes.Length)
      }
      finally {
        $res.Close()
      }
    }
  }
  finally {
    $listener.Stop()
    $listener.Close()
  }
}

if (-not (Start-PythonStaticServer)) {
  Write-Host "Python not found; using built-in static server (slower, fine for local playtests)."
  Start-PowerShellStaticServer
}
