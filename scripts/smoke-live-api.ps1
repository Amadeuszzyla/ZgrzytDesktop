#Requires -Version 5.1
<#
.SYNOPSIS
    Live API smoke test for ZGRZYT (GET /api/user, optional POST /api/login).

.DESCRIPTION
    Safe against null WebException.Response (timeout, DNS, TLS, no HTTP response).
    Never logs passwords or access tokens.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$DefaultApiBaseUrl = "https://zgrzyt-api.onrender.com/api/"
$RequestTimeoutMs = 120000

$script:FailureCount = 0
$script:UserTestOutcome = $null
$script:LoginTestOutcome = "skipped"

function Write-SmokeLine {
    param(
        [string]$Level,
        [string]$Message
    )
    Write-Host "[$Level] $Message"
}

function Add-Failure {
    $script:FailureCount++
}

function Get-ApiBaseUrl {
    $raw = $env:ZGRZYT_API_URL
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $DefaultApiBaseUrl
    }

    $normalized = $raw.Trim()
    if (-not ($normalized.StartsWith("http://", [StringComparison]::OrdinalIgnoreCase) -or
            $normalized.StartsWith("https://", [StringComparison]::OrdinalIgnoreCase))) {
        $normalized = "https://$normalized"
    }

    if (-not $normalized.EndsWith("/")) {
        $normalized += "/"
    }

    if (-not $normalized.EndsWith("api/", [StringComparison]::OrdinalIgnoreCase)) {
        $normalized += "api/"
    }

    $normalized = $normalized -replace "api/api/", "api/", "IgnoreCase"
    return $normalized
}

function Join-ApiUri {
    param(
        [string]$BaseUrl,
        [string]$Endpoint
    )
    $ep = $Endpoint.TrimStart("/")
    return ($BaseUrl + $ep)
}

function Get-BodyKind {
    param([string]$Body)

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return "empty"
    }

    $trimmed = $Body.Trim()
    if ($trimmed.StartsWith("{") -or $trimmed.StartsWith("[")) {
        try {
            $null = $trimmed | ConvertFrom-Json -ErrorAction Stop
            return "json"
        }
        catch {
            return "text"
        }
    }

    if ($trimmed -match '(?is)<!DOCTYPE\s+html|<html[\s>]') {
        return "html"
    }

    return "text"
}

function Get-SafeBodyPreview {
    param(
        [string]$Body,
        [int]$MaxLength = 200
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return "(empty body)"
    }

    $preview = $Body
    $preview = $preview -replace '(?i)"access_token"\s*:\s*"[^"]*"', '"access_token":"[redacted]"'
    $preview = $preview -replace '(?i)"token"\s*:\s*"[^"]*"', '"token":"[redacted]"'
    $preview = $preview -replace '(?i)"password"\s*:\s*"[^"]*"', '"password":"[redacted]"'

    if ($preview.Length -gt $MaxLength) {
        $preview = $preview.Substring(0, $MaxLength) + "..."
    }

    return $preview
}

function Read-HttpBody {
    param([System.IO.Stream]$Stream)

    if ($null -eq $Stream) {
        return ""
    }

    try {
        $reader = New-Object System.IO.StreamReader($Stream)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    catch {
        return ""
    }
    finally {
        try { $Stream.Dispose() } catch { }
    }
}

function Get-NoResponseErrorKind {
    param([System.Net.WebException]$WebException)

    switch ($WebException.Status) {
        ([System.Net.WebExceptionStatus]::Timeout) { return "timeout" }
        ([System.Net.WebExceptionStatus]::NameResolutionFailure) { return "dns" }
        ([System.Net.WebExceptionStatus]::TrustFailure) { return "tls" }
        ([System.Net.WebExceptionStatus]::SecureChannelFailure) { return "tls" }
        ([System.Net.WebExceptionStatus]::ConnectFailure) { return "connection" }
        ([System.Net.WebExceptionStatus]::SendFailure) { return "connection" }
        ([System.Net.WebExceptionStatus]::ReceiveFailure) { return "connection" }
        default { return "no_http_response" }
    }
}

function Invoke-ApiSmokeRequest {
    param(
        [ValidateSet("GET", "POST")]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [string]$JsonBody = $null
    )

    $outcome = [ordered]@{
        HasHttpResponse = $false
        StatusCode      = $null
        StatusLine      = ""
        Body            = ""
        BodyKind        = "empty"
        BodyPreview     = "(empty body)"
        ErrorKind       = "none"
        ErrorMessage    = ""
    }

    $request = $null
    $response = $null

    try {
        $request = [System.Net.HttpWebRequest]::Create($Uri)
        $request.Method = $Method
        $request.Timeout = $RequestTimeoutMs
        $request.ReadWriteTimeout = $RequestTimeoutMs
        $request.Accept = "application/json"
        $request.ContentType = "application/json"
        $request.UserAgent = "ZgrzytDesktop-smoke-live-api/1.0"

        if ($Method -eq "POST" -and $null -ne $JsonBody) {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($JsonBody)
            $request.ContentLength = $bytes.Length
            $stream = $request.GetRequestStream()
            try {
                $stream.Write($bytes, 0, $bytes.Length)
            }
            finally {
                $stream.Dispose()
            }
        }

        try {
            $response = $request.GetResponse()
        }
        catch [System.Net.WebException] {
            $errorResponse = $_.Exception.Response
            if ($null -eq $errorResponse) {
                $outcome.ErrorKind = Get-NoResponseErrorKind -WebException $_.Exception
                $outcome.ErrorMessage = $_.Exception.Message
                return [pscustomobject]$outcome
            }

            $response = $errorResponse
        }

        $outcome.HasHttpResponse = $true
        $outcome.StatusCode = [int]$response.StatusCode
        $outcome.StatusLine = "$($response.StatusCode) $($response.StatusDescription)".Trim()

        $bodyStream = $null
        try {
            $bodyStream = $response.GetResponseStream()
        }
        catch {
            $bodyStream = $null
        }

        $outcome.Body = Read-HttpBody -Stream $bodyStream
        $outcome.BodyKind = Get-BodyKind -Body $outcome.Body
        $outcome.BodyPreview = Get-SafeBodyPreview -Body $outcome.Body
        return [pscustomobject]$outcome
    }
    catch [System.Net.WebException] {
        if ($null -ne $_.Exception.Response) {
            $response = $_.Exception.Response
            $outcome.HasHttpResponse = $true
            $outcome.StatusCode = [int]$response.StatusCode
            $outcome.StatusLine = "$($response.StatusCode) $($response.StatusDescription)".Trim()

            $bodyStream = $null
            try {
                $bodyStream = $response.GetResponseStream()
            }
            catch {
                $bodyStream = $null
            }

            $outcome.Body = Read-HttpBody -Stream $bodyStream
            $outcome.BodyKind = Get-BodyKind -Body $outcome.Body
            $outcome.BodyPreview = Get-SafeBodyPreview -Body $outcome.Body
            return [pscustomobject]$outcome
        }

        $outcome.ErrorKind = Get-NoResponseErrorKind -WebException $_.Exception
        $outcome.ErrorMessage = $_.Exception.Message
        return [pscustomobject]$outcome
    }
    catch {
        $outcome.ErrorKind = "other"
        $outcome.ErrorMessage = $_.Exception.Message
        return [pscustomobject]$outcome
    }
    finally {
        if ($null -ne $response) {
            try { $response.Close() } catch { }
            try { $response.Dispose() } catch { }
        }
    }
}

function Format-ConnectionDetail {
    param([pscustomobject]$Result)

    $detail = "error_kind=$($Result.ErrorKind)"
    if (-not [string]::IsNullOrWhiteSpace($Result.ErrorMessage)) {
        $detail += "; message=$($Result.ErrorMessage)"
    }
    return $detail
}

function Test-UserWithoutToken {
    param([string]$BaseUrl)

    $uri = Join-ApiUri -BaseUrl $BaseUrl -Endpoint "user"
    Write-SmokeLine -Level "INFO" -Message "Test 1: GET $uri (no Bearer token)"

    $result = Invoke-ApiSmokeRequest -Method "GET" -Uri $uri

    if (-not $result.HasHttpResponse) {
        $detail = Format-ConnectionDetail -Result $result
        Write-SmokeLine -Level "INFO" -Message "No HTTP response ($detail). Possible cold start on Render, DNS, TLS, or timeout (~$([int]($RequestTimeoutMs / 1000))s). Retry in 30-60s."
        Write-SmokeLine -Level "FAIL" -Message "GET /api/user - connection problem (no HTTP status)."
        $script:UserTestOutcome = "connection_fail"
        Add-Failure
        return
    }

    $code = $result.StatusCode
    Write-SmokeLine -Level "INFO" -Message "HTTP $code ($($result.StatusLine)); body_kind=$($result.BodyKind); preview=$($result.BodyPreview)"

    if ($code -eq 401) {
        Write-SmokeLine -Level "PASS" -Message "GET /api/user returned 401 - API is alive and requires authentication."
        $script:UserTestOutcome = "pass_401"
        return
    }

    if ($code -ge 500) {
        Write-SmokeLine -Level "FAIL" -Message "GET /api/user returned $code - backend error."
        $script:UserTestOutcome = "fail_backend_$code"
        Add-Failure
        return
    }

    Write-SmokeLine -Level "FAIL" -Message "GET /api/user returned $code - expected 401 without token."
    $script:UserTestOutcome = "fail_unexpected_$code"
    Add-Failure
}

function Test-Login {
    param([string]$BaseUrl)

    $login = $env:ZGRZYT_LOGIN
    $password = $env:ZGRZYT_PASSWORD

    if ([string]::IsNullOrWhiteSpace($login) -or [string]::IsNullOrWhiteSpace($password)) {
        Write-SmokeLine -Level "INFO" -Message "Login smoke skipped because ZGRZYT_LOGIN/ZGRZYT_PASSWORD are not set."
        $script:LoginTestOutcome = "skipped"
        return
    }

    $script:LoginTestOutcome = "executed"
    $uri = Join-ApiUri -BaseUrl $BaseUrl -Endpoint "login"
    Write-SmokeLine -Level "INFO" -Message "Test 2: POST $uri (login=$login; password not logged)"

    $payload = (@{ login = $login.Trim(); password = $password } | ConvertTo-Json -Compress)
    $result = Invoke-ApiSmokeRequest -Method "POST" -Uri $uri -JsonBody $payload

    if (-not $result.HasHttpResponse) {
        $detail = Format-ConnectionDetail -Result $result
        Write-SmokeLine -Level "FAIL" -Message "POST /api/login - connection problem (no HTTP status). $detail"
        $script:LoginTestOutcome = "connection_fail"
        Add-Failure
        return
    }

    $code = $result.StatusCode
    if ($code -eq 200) {
        Write-SmokeLine -Level "PASS" -Message "POST /api/login returned 200 - credentials accepted (response body not logged)."
        $script:LoginTestOutcome = "pass_200"
        return
    }

    Write-SmokeLine -Level "INFO" -Message "HTTP $code ($($result.StatusLine)); body_kind=$($result.BodyKind); preview=$($result.BodyPreview)"

    switch ($code) {
        401 {
            Write-SmokeLine -Level "FAIL" -Message "POST /api/login returned 401 - invalid credentials."
            $script:LoginTestOutcome = "fail_401_invalid_credentials"
            Add-Failure
        }
        403 {
            Write-SmokeLine -Level "FAIL" -Message "POST /api/login returned 403 - no desktop/API access for this account."
            $script:LoginTestOutcome = "fail_403_no_access"
            Add-Failure
        }
        422 {
            Write-SmokeLine -Level "FAIL" -Message "POST /api/login returned 422 - validation error."
            $script:LoginTestOutcome = "fail_422_validation"
            Add-Failure
        }
        default {
            if ($code -ge 500) {
                Write-SmokeLine -Level "FAIL" -Message "POST /api/login returned $code - backend error."
                $script:LoginTestOutcome = "fail_backend_$code"
            }
            else {
                Write-SmokeLine -Level "FAIL" -Message "POST /api/login returned $code - unexpected status."
                $script:LoginTestOutcome = "fail_unexpected_$code"
            }
            Add-Failure
        }
    }
}

# --- main ---
$baseUrl = Get-ApiBaseUrl
Write-SmokeLine -Level "INFO" -Message "ZGRZYT live API smoke"
Write-SmokeLine -Level "INFO" -Message "API base: $baseUrl"
Write-SmokeLine -Level "INFO" -Message "Request timeout: $([int]($RequestTimeoutMs / 1000))s"

Test-UserWithoutToken -BaseUrl $baseUrl
Test-Login -BaseUrl $baseUrl

Write-Host ""
Write-SmokeLine -Level "INFO" -Message "Summary: GET /api/user outcome=$($script:UserTestOutcome); login smoke=$($script:LoginTestOutcome); failures=$($script:FailureCount)"

if ($script:FailureCount -gt 0) {
    exit 1
}

exit 0
