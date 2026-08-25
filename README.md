# Business Model Management System

A comprehensive business management platform with role-based access control and AI-driven insights.

## Project Structure

```
BusinessModelApp/
├── src/
│   ├── BusinessModelApp.API/         # .NET Web API
│   ├── BusinessModelApp.Core/         # Core domain models and interfaces
│   ├── BusinessModelApp.Infrastructure/ # Data access and external services
│   └── BusinessModelApp.Web/          # React frontend
├── tests/
│   ├── BusinessModelApp.API.Tests/    # API integration tests
│   └── BusinessModelApp.Core.Tests/   # Unit tests
└── docs/                             # Project documentation
```

## Prerequisites

1. .NET 8.0 SDK
2. Node.js 18+ and npm
3. PostgreSQL 15+
4. Visual Studio 2022 / VS Code

## Getting Started

### 1. Install .NET 8.0 SDK
Download and install from: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Install Node.js
Download and install from: https://nodejs.org/

### 3. Set up PostgreSQL
Download and install from: https://www.postgresql.org/download/

## Development Setup

### Backend Setup
```bash
cd src/BusinessModelApp.API
dotnet restore
dotnet run
```

### Frontend Setup
```bash
cd src/BusinessModelApp.Web
npm install
npm start
```

## License
MIT
