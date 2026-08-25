# OmniRoute Integration & Operations Guide

## 1. Gateway Configuration

Configure the OmniRoute endpoint in `appsettings.json` or through environment variables:

```json
{
  "AI": {
    "Gateway": {
      "Provider": "OmniRoute",
      "BaseUrl": "http://localhost:8000",
      "ApiKey": "YOUR_OMNIROUTE_API_KEY",
      "TimeoutSeconds": 60,
      "EnableCaching": true
    }
  }
}
```

Environment Variable Overrides:
```bash
AI__Gateway__BaseUrl=https://omniroute.internal.net
AI__Gateway__ApiKey=sec_omniroute_token_xxx
```

---

## 2. Health Monitoring & Verification

OmniRoute exposes a provider-neutral health check:
```http
GET /health
```
When healthy, OmniRoute returns `HTTP 200` (`{"status": "healthy"}`).

The `IOmniRouteClient.CheckHealthAsync()` method integrates directly into internal system health diagnostics.
