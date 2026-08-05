# Computer Seekho Project

# Security Microservice Integration Guide

**Version:** 1.0
**Applicable To:** All Backend & Frontend Developers

---

# Overview

This project follows a **Microservice Architecture** where **Authentication and Authorization** are completely separated from the Business Application.

Every developer is responsible only for their own business modules (Enquiries, Content, Follow-up, Gallery, etc.).

Authentication is handled by the **Security Microservice**.

---

# System Architecture

```text
                        ┌────────────────────────────┐
                        │     React Frontend         │
                        │        Port : 5137         │
                        └─────────────┬──────────────┘
                                      │
                                      │ Login Request
                                      ▼
                   ┌────────────────────────────────────┐
                   │ Security Microservice (Docker)     │
                   │ Port : 8137                        │
                   │                                    │
                   │ Staff Login API                    │
                   │ POST /api/staff/login             │
                   │                                    │
                   │ SuperAdmin Login API              │
                   │ POST /api/admin/login             │
                   └───────────────┬────────────────────┘
                                   │
                          Returns JWT Token
                                   │
                                   ▼
                     Token stored in Browser (LocalStorage)
                                   │
                                   │
                                   ▼
          Every Business API Request includes:
          Authorization: Bearer <JWT Token>

                                   │
                                   ▼
             ┌──────────────────────────────────────────┐
             │ Business Middleware (.NET API)           │
             │ Port : 8097                              │
             │                                          │
             │ Modules                                 │
             │ • Enquiries                             │
             │ • Content Manager                       │
             │ • Follow-ups                            │
             │ • Gallery                               │
             │ • Admissions                            │
             │ • Any Future Modules                    │
             └───────────────────┬──────────────────────┘
                                 │
                                 ▼
                    Business MySQL Database (Port 7809)
```

---

# Project Responsibility

## Security Team

Responsible for:

* User Authentication
* User Authorization
* JWT Token Generation
* Role Management
* Password Security
* Staff Management
* SuperAdmin Management

Security Database is completely isolated.

Business developers DO NOT modify Security Database.

---

## Business Team

Responsible for:

* Enquiries
* Content Management
* Follow Ups
* Student Management
* Reports
* Admissions
* Gallery
* Future Features

Business developers only need to validate the JWT Token.

They never create or generate tokens.

---

# Login Flow

```text
React App

        │

        │ POST /api/staff/login

        ▼

Security Service

        │

        │ Validate User

        │

        ▼

Generate JWT Token

        │

        ▼

Return Token

        │

        ▼

React stores token

        │

        ▼

All future API calls

Authorization: Bearer <token>
```

---

# API Endpoints

## Staff Login

```
POST http://localhost:8137/api/staff/login
```

For:

* Counselor
* Content Manager
* Staff
* Operations
* Marketing

---

## SuperAdmin Login

```
POST http://localhost:8137/api/admin/login
```

Only SuperAdmin can login here.

---

# Test Accounts

| Role            | Email                                                               | Password      |
| --------------- | ------------------------------------------------------------------- | ------------- |
| SuperAdmin      | [admin@computerseekho.com](mailto:admin@computerseekho.com)         | Admin@1234    |
| Counselor       | [counselor@computerseekho.com](mailto:counselor@computerseekho.com) | Password@1234 |
| Content Manager | [content@computerseekho.com](mailto:content@computerseekho.com)     | Password@1234 |
| Staff           | [staff@computerseekho.com](mailto:staff@computerseekho.com)         | Password@1234 |

---

# Business Middleware Integration

Every backend developer must complete the following steps.

---

## Step 1

### Configure appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "SuperSecretKeyAtLeast256BitsLongForHmacSha256Security!",
    "Issuer": "Identity.API",
    "Audience": "ComputerSeekho.Client"
  }
}
```

These values **must exactly match** the Security Microservice.

Changing these values will invalidate authentication.

---

## Step 2

### Configure JWT Authentication

Program.cs

```csharp
builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = "Identity.API",
        ValidAudience = "ComputerSeekho.Client",

        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])
            )
    };
});
```

---

## Step 3

### Configure CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder
            .WithOrigins("http://localhost:5137")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

---

## Step 4

### Middleware Pipeline

The middleware order is important.

```csharp
app.UseCors("AllowReactApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
```

---

# Protect Controllers

Every secured controller should include:

```csharp
[Authorize]
```

Example

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnquiryController : ControllerBase
{

}
```

---

# Restrict by Roles

If only specific staff should access a controller:

```csharp
[Authorize(Roles = "Counselor")]
```

Multiple roles:

```csharp
[Authorize(Roles="Counselor,ContentManager,SuperAdmin")]
```

Only SuperAdmin

```csharp
[Authorize(Roles="SuperAdmin")]
```

---

# React Integration

Two API clients are required.

## Security API

```javascript
http://localhost:8137/api
```

Used only for:

* Login
* Logout
* Password
* Staff Profile

---

## Business API

```javascript
http://localhost:8097/api
```

Used for:

* Enquiries
* Students
* Content
* Gallery
* Reports
* Admissions

---

# Axios Configuration

```javascript
securityApi

↓

Login

↓

Receive Token

↓

Save Token

↓

businessApi

↓

Automatically send

Authorization: Bearer <JWT>
```

---

# Business Developer Workflow

```
Create Controller

↓

Add [Authorize]

↓

Write Business Logic

↓

Use Business Database

↓

Done
```

Business developers should never:

* Create JWT Tokens
* Validate Passwords
* Manage Users
* Modify Security Database

Those responsibilities belong to the Security Microservice.

---

# Example Request Flow

## User Login

```
React

↓

POST /staff/login

↓

Security API

↓

Validate Credentials

↓

Generate JWT

↓

Return Token
```

---

## Create Enquiry

```
React

↓

POST /api/enquiry

Authorization: Bearer JWT

↓

Business Middleware

↓

Validate JWT

↓

Execute Business Logic

↓

Save into MySQL

↓

Return Success
```

---

# Responsibilities Summary

| Component                  | Responsibility                                  |
| -------------------------- | ----------------------------------------------- |
| React (5137)               | User Interface, Login, Send JWT                 |
| Security Service (8137)    | Authentication, Authorization, JWT Generation   |
| Business Middleware (8097) | Business Logic, Validate JWT, Role-Based Access |
| Business Database (7809)   | Business Data                                   |
| Security Database          | Users, Roles, Permissions                       |

---

# Important Rules

✅ Always login through the Security Microservice.

✅ Never create users inside Business Middleware.

✅ Never generate JWT Tokens inside Business Middleware.

✅ Always protect controllers using `[Authorize]`.

✅ Use role-based authorization where required.

✅ Keep business data and security data in separate databases.

---

# Future Modules

Any future module (Attendance, Fees, Notifications, CRM, LMS, etc.) should follow the same integration pattern:

```
React

↓

Security Service

↓

JWT Token

↓

Business Module

↓

Business Database
```

This architecture ensures that all modules share a centralized authentication system while allowing each team to develop and maintain business features independently.





this is much sufficient





---------------------------normal installation ----------------------------------------------------------------------
without docker 



Here is the step-by-step guide to push your code to GitHub, followed by a complete `README.md` formatted specifically for running your solution on both **Arch Linux** and **Windows**.

---

## Part 1: Push Project to GitHub

Run these commands in your project root directory (`/home/ashish/Identity.Solution`):

```bash
# 1. Initialize Git repository
git init

# 2. Add all files to staging
git add .

# 3. Create initial commit
git commit -m "feat: complete identity API with portal separation and MySQL integration"

# 4. Set default branch to main
git branch -M main

# 5. Add remote GitHub repository
git remote add origin https://github.com/ashu304-ops/security.git

# 6. Push code to GitHub
git push -u origin main

```

*(If prompted for credentials, enter your GitHub Username and Personal Access Token or SSH Key).*

---

## Part 2: `README.md` File

Create a file named `README.md` in your project root directory and paste the following content:

```markdown
# Computer Seekho - Identity & Access Management (IAM) Solution

A modular, clean-architecture ASP.NET Core 9 Web API providing Identity & Access Management (IAM) with Role-Based Access Control (RBAC), JWT authentication, and isolated front-end admin/staff management portals.

---

## 🛠 Tech Stack

- **Backend:** .NET 9 Web API (Clean Architecture: API, Application, Domain, Infrastructure, Persistence, Contracts)
- **Database:** MySQL
- **Authentication:** ASP.NET Core Identity + JWT Tokens
- **Frontend:** Vanilla JS / HTML5 / CSS3 (Hosted in `wwwroot`)

---

## 🚀 Getting Started

Follow the instructions below depending on your operating system.

### Prerequisites (All Platforms)
1. **.NET 9 SDK** installed.
2. **MySQL Server** running locally or in Docker.

---

## 🐧 Option A: Setup on Arch Linux

### 1. Install Dependencies
```bash
sudo pacman -S dotnet-sdk mysql

```

### 2. Start MySQL Service

```bash
sudo systemctl enable --now mysqld

```

### 3. Setup Database and User

Log into MySQL and execute:

```sql
CREATE DATABASE IF NOT EXISTS ComputerSeekhoIdentityDb;
CREATE USER IF NOT EXISTS 'root'@'localhost' IDENTIFIED BY 'root';
GRANT ALL PRIVILEGES ON ComputerSeekhoIdentityDb.* TO 'root'@'localhost';
FLUSH PRIVILEGES;

```

### 4. Configure Connection String

Verify `src/Presentation/Identity.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ComputerSeekhoIdentityDb;User=root;Password=root;"
}

```

### 5. Restore, Build, and Run

```bash
cd Identity.Solution

# Restore NuGet dependencies
dotnet restore

# Build the solution
dotnet build

# Apply EF Core Migrations
dotnet ef database update --project src/Infrastructure/Identity.Persistence --startup-project src/Presentation/Identity.API

# Run Web API project
dotnet run --project src/Presentation/Identity.API

```

---

## 🪟 Option B: Setup on Windows

### 1. Install Prerequisites

* Download and install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
* Download and install [MySQL Community Server](https://dev.mysql.com/downloads/installer/).

### 2. Setup Database in MySQL Workbench / Command Line

```sql
CREATE DATABASE IF NOT EXISTS ComputerSeekhoIdentityDb;

```

### 3. Configure Connection String

Update `src\Presentation\Identity.API\appsettings.json` with your MySQL credentials:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ComputerSeekhoIdentityDb;User=root;Password=your_mysql_password;"
}

```

### 4. Restore, Build, and Run (PowerShell or CMD)

```powershell
# Navigate to solution directory
cd Identity.Solution

# Restore packages
dotnet restore

# Build solution
dotnet build

# Apply database migrations
dotnet ef database update --project src\Infrastructure\Identity.Persistence --startup-project src\Presentation\Identity.API

# Run application
dotnet run --project src\Presentation\Identity.API

```

---

## 🌐 Accessing Portals

Once the API starts (`http://localhost:5097` or `https://localhost:7097`):

| Portal | URL | Authorized Roles | Description |
| --- | --- | --- | --- |
| **SuperAdmin Portal** | `http://localhost:5097/admin.html` | `SuperAdmin` | IAM controls, role creation, staff registration |
| **Staff Workspace** | `http://localhost:5097/staff.html` | Staff (Counselor, Manager, etc.) | Operational dashboard for enquiries & registrations |

---

## 🧪 Running Unit Tests

Run test suites on any OS:

```bash
dotnet test

```

```

<FollowUp label="Would you like to generate a .gitignore file tailored for .NET and Linux/Windows to avoid committing build outputs?" query="Create a standard .gitignore file for .NET projects."/>

```
