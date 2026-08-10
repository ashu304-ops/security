
---

# Computer Seekho — Security IAM Service

The **Identity and Access Management (IAM) Service** is the centralized authentication and authorization provider for the Computer Seekho enterprise ecosystem. It handles user credentials, role-based access control (RBAC), fine-grained permissions, and Two-Factor Authentication (2FA/MFA) for administrative staff.

---

## 1. Authentication & System Flow

```
                                  +-----------------------+
                                  | User visits staff.html|
                                  +-----------+-----------+
                                              |
                                   Enters Email & Password
                                              |
                                              v
                                  +-----------------------+
                                  |  POST /api/Auth/login |
                                  +-----------+-----------+
                                              |
                          Is SuperAdmin?      |      Regular Staff?
                   +--------------------------+--------------------------+
                   |                                                     |
                   v                                                     v
     +--------------------------+                          +---------------------------+
     | Requires 2FA             |                          | Issues JWT Access Token   |
     | Redirects to admin.html  |                          | Redirects to React App    |
     +------------+-------------+                          | (localhost:5173/dashboard)|
                  |                                        +---------------------------+
       Enters 6-Digit Code
                  |
                  v
     +--------------------------+
     | Issues JWT Access Token  |
     | Opens Admin Portal UI    |
     +--------------------------+

```

---

## 2. System Architecture & Conceptual Overview

### How Authentication Works

1. **Credentials Verification**: Users submit credentials via `staff.html`.
2. **2FA Gate**: SuperAdmin accounts require a 6-digit TOTP verification code from Google or Microsoft Authenticator before a token is granted.
3. **JWT Token Issuance**: On successful verification, the IAM service returns a cryptographically signed **JSON Web Token (JWT)** containing user claims, roles, and permissions.
4. **Role Routing**:
* **SuperAdmins** are directed to the SuperAdmin Management Portal (`/admin.html`).
* **Staff Members** (e.g., Counselors, Content Managers) are redirected to the React Operations App (`http://localhost:5173/admin/dashboard?token=...`).



### Identity Database Schema

The backend uses **ASP.NET Core Identity** backed by **MySQL 8.0**:

```
┌───────────────┐        ┌──────────────────┐        ┌───────────────┐
│     Users     │◄──────►│    UserRoles     │◄──────►│     Roles     │
└───────────────┘        └──────────────────┘        └───────┬───────┘
                                                             │
                                                             ▼
                                                    ┌──────────────────┐
                                                    │ RolePermissions  │
                                                    └───────┬──────────┘
                                                            │
                                                            ▼
                                                    ┌──────────────────┐
                                                    │   Permissions    │
                                                    └──────────────────┘

```

---

## 3. Default System Roles & Seeded Accounts

When the system boots up for the first time, EF Core automatically applies migrations and seeds default permissions, roles, and accounts:

| Persona | Role | Assigned Permissions | Default Email | Default Password |
| --- | --- | --- | --- | --- |
| **SuperAdmin** | `SuperAdmin` | Full System Access (`Staff.Manage`, `Course.CRUD`, `Enquiry.CRUD`, etc.) | `admin@computerseekho.com` | `Admin@1234` |
| **Content Manager** | `ContentManager` | `Course.CRUD`, `Table.Maintenance`, `Excel.Upload` | *Created via Admin UI* | *Set at creation* |
| **Counselor** | `Counselor` | `Enquiry.CRUD`, `FollowUp.View`, `Student.Register`, `Payment.Process` | *Created via Admin UI* | *Set at creation* |

---

## 4. Local Development Quickstart

### Prerequisites

* [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.
* [Git](https://git-scm.com/).

### Running the Stack via Docker Compose

Clone the repository and launch the containerized stack:

```bash
# Spin up MySQL and the IAM Backend
docker compose up -d

# Verify container statuses
docker compose ps

```

### Accessing Local Portals

* **Staff Login Portal**: [http://localhost:8137/staff.html](http://localhost:8137/staff.html)
* **SuperAdmin IAM Dashboard**: [http://localhost:8137/admin.html](http://localhost:8137/admin.html)

---

## 5. API Reference & Backend Integration (Java / Other Microservices)

### REST API Endpoints Reference

| Method | Endpoint | Authorization | Description | Request Body / Query Params |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Auth/login` | Anonymous | Authenticates staff and SuperAdmin credentials | `{"emailOrUsername": "...", "password": "..."}` |
| `POST` | `/api/Auth/verify-mfa` | Anonymous | Validates 6-digit TOTP authenticator code | `{"emailOrUsername": "...", "code": "123456"}` |
| `POST` | `/api/Auth/enable-mfa` | `[Authorize]` | Generates MFA secret key & QR Code URI | `{"emailOrUsername": "..."}` |
| `POST` | `/api/Auth/register-staff` | `[Authorize(SuperAdmin)]` | Registers new staff members and assigns initial roles | `{"username": "...", "staffName": "...", "email": "...", "department": "...", "roles": ["..."], "password": "..."}` |
| `GET` | `/api/Staff` | `[Authorize]` | Retrieves list of all registered staff members | *None* |
| `POST` | `/api/Staff/{id}/assign-role` | `[Authorize(SuperAdmin)]` | Updates assigned roles for a specific user ID | `{"roles": ["ContentManager", "Counselor"]}` |
| `DELETE` | `/api/Staff/{id}` | `[Authorize(SuperAdmin)]` | Deletes a staff account from database | *None* |
| `GET` | `/api/Role` | `[Authorize]` | Fetches all system roles (`ContentManager`, `Counselor`, etc.) | *None* |
| `POST` | `/api/Role` | `[Authorize(SuperAdmin)]` | Creates a new role with associated permission IDs | `{"roleName": "...", "description": "...", "permissionIds": [1, 2]}` |
| `GET` | `/api/Permission` | `[Authorize]` | Retrieves all 8 system permissions | *None* |

---

### Key Technical Details for Java & Secondary Microservice Developers

#### 1. Token Generation Mechanism

The .NET IAM service handles all credential verification and password hashing (PBKDF2), issuing **HMAC-SHA256 signed JWT access tokens**.

#### 2. Stateless Java `JwtAuthenticationFilter` Validation

Secondary microservices (e.g., Java Spring Boot backend) **do not need to perform an HTTP request back to the IAM service** for token validation. Instead, validate the incoming JWT statelessly in memory using the shared cryptographic key and issuer properties:

* **Algorithm**: `HMAC-SHA256` (`HS256`)
* **Secret Key**: `SuperSecretKeyAtLeast256BitsLongForHmacSha256Security!`
* **Issuer (`iss`)**: `Identity.API`
* **Audience (`aud`)**: `ComputerSeekho.Client`
* **Claims Structure**:
* `sub` or `NameIdentifier`: User GUID (`Id`)
* `email`: User Email
* `[http://schemas.microsoft.com/ws/2008/06/identity/claims/role](http://schemas.microsoft.com/ws/2008/06/identity/claims/role)` or `role`: Assigned Role string (e.g., `ContentManager`, `Counselor`)
* `Permission`: Array of granted permission strings (e.g., `["Course.CRUD", "Table.Maintenance"]`)



##### Example Java Spring Security Configuration (`application.yml`):

```yaml
jwt:
  secret: "SuperSecretKeyAtLeast256BitsLongForHmacSha256Security!"
  issuer: "Identity.API"
  audience: "ComputerSeekho.Client"

```

#### 3. Database Coexistence & Isolation

* **Database Name**: `ComputerSeekhoDb`
* **Isolation Strategy**: The IAM service seeds and manages its dedicated identity tables (`Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`, `AuditLogs`, `RefreshTokens`) inside `ComputerSeekhoDb`.
* Other microservices (e.g., Java APIs) connect to `ComputerSeekhoDb` to manage domain entities (`Courses`, `Enquiries`, `Batches`, `Payments`) without interfering with authentication tables.

#### 4. Docker Service Resolution & Ports

When resolving services inside the shared Docker network (`cs-network`):

* **IAM Backend Endpoint**:
* **Internal Container Network**: `http://cs-iam-backend:8137`
* **Host Machine Access**: `http://localhost:8137`


* **MySQL Database Endpoint**:
* **Internal Container Network**: `cs-iam-mysql:3006`
* **Host Machine Access**: `localhost:3310`



##### Java Container Database Configuration (`application.properties`):

```properties
spring.datasource.url=jdbc:mysql://cs-iam-mysql:3006/ComputerSeekhoDb?useSSL=false&allowPublicKeyRetrieval=true&serverTimezone=UTC
spring.datasource.username=root
spring.datasource.password=Ashu@1234

```

---

## 6. Frontend Integration Guide (React - `localhost:5173`)

### Step 1: Handling Token Intake Query String

When a staff member logs in, they are redirected to the React frontend with their token attached to the URL query parameter (`?token=...`). Add this hook to your React application (`App.jsx` or Auth Context):

```jsx
import { useEffect, useState } from 'react';

export function useAuth() {
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [user, setUser] = useState(null);

  useEffect(() => {
    // Extract token from URL query string
    const urlParams = new URLSearchParams(window.location.search);
    const tokenFromUrl = urlParams.get('token');

    let activeToken = token;

    if (tokenFromUrl) {
      activeToken = tokenFromUrl;
      localStorage.setItem('token', tokenFromUrl);
      setToken(tokenFromUrl);
      
      // Clean query parameter from address bar
      window.history.replaceState({}, document.title, window.location.pathname);
    }

    // Decode claims from JWT payload
    if (activeToken) {
      try {
        const payloadBase64 = activeToken.split('.')[1];
        const decodedPayload = JSON.parse(atob(payloadBase64));
        setUser(decodedPayload);
      } catch (err) {
        console.error('Invalid token format:', err);
      }
    }
  }, []);

  return { token, user };
}

```

### Step 2: Attaching Authorization Headers

Attach the stored token to all outgoing API requests from the React client:

```javascript
async function fetchCourseData() {
  const token = localStorage.getItem('token');

  const response = await fetch('http://localhost:8137/api/Course', {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });

  return await response.json();
}

```

---


      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      cs-iam-mysql:
        condition: service_healthy

```
