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
