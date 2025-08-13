# User Management API

## Overview

The **User Management API** is a minimal ASP.NET Core 8 application built using the Minimal API approach with Entity Framework Core InMemory for persistence. It provides endpoints for creating, reading, updating, and deleting users while ensuring robust validation, centralized error handling, request/response logging, and JWT-based authentication.

This project was developed iteratively with strong emphasis on production-readiness, even though it uses an in-memory database for demonstration. The architecture was designed for clarity, maintainability, and scalability — making it a solid base for real-world scenarios by swapping the database provider or integrating into a larger system.

---

## Features

- **Minimal API architecture** for fast and clean endpoint definition.
- **EF Core InMemory database** for quick prototyping and testing.
- **DTO-based API contracts** to avoid leaking domain models.
- **FluentValidation** for input validation, including:
  - Name, Email, and Password rules.
  - Uniqueness checks for Email.
- **Centralized error handling middleware** returning consistent RFC 7807 ProblemDetails.
- **Request/Response logging middleware** with:
  - HTTP method, path, status code.
  - Full request/response bodies.
  - Execution time.
  - Per-endpoint request counters.
- **JWT Authentication middleware** to secure endpoints.
- **Swagger/OpenAPI documentation** with:
  - Examples for requests and responses.
  - Tagged and summarized endpoints.
  - Auth integration in Swagger UI for JWT bearer tokens.
- **Seed data** for initial testing.
- **Conflict handling** (`409`) for duplicate emails.

---

## Project Structure

```
UserManagementAPI/
│
├── Api/
│   ├── Endpoints/
│   │   └── UsersEndpoints.cs          # All user CRUD endpoints
│   ├── Middlewares/
│   │   ├── ErrorHandlingMiddleware.cs # Centralized exception handler
│   │   ├── RequestResponseLoggingMiddleware.cs
│   │   └── AuthenticationMiddleware.cs
│
├── Data/
│   └── AppDbContext.cs                # EF Core DbContext
│
├── DTOs/
│   ├── CreateUserDto.cs
│   ├── UpdateUserDto.cs
│   └── UserDto.cs
│
├── Models/
│   └── User.cs                        # Domain model
│
├── Validators/
│   ├── CreateUserDtoValidator.cs
│   └── UpdateUserDtoValidator.cs
│
├── Program.cs                         # App configuration & pipeline
└── README.md
```

---

## Endpoints

### Authentication

- **POST `/login`** – Validates credentials and issues a JWT token.
  - JWT tokens are signed with a 256-bit secret key (minimum size for HS256).

### Users

- **GET `/users`** – List all users.
- **GET `/users/{id}`** – Get a single user.
- **POST `/users`** – Create a new user (requires unique email, strong password).
- **PUT `/users/{id}`** – Update user (`409` if email already exists).
- **DELETE `/users/{id}`** – Delete a user.

---

## Running the Project

1. **Clone the repository**

    ```bash
    git clone https://github.com/walidmoustafa2077/UserManagementAPI.git
    cd UserManagementAPI
    ```

2. **Install dependencies**

    ```bash
    dotnet restore
    ```

3. **Run the API**

    ```bash
    dotnet run
    ```

4. **Open Swagger UI at:**

    ```
    http://localhost:5069/swagger
    ```

---

## Authentication in Swagger

- Generate a token:
  - Use `/login` with valid credentials (e.g., username: `admin`, password: `1234`).
  - Copy the JWT from the response.
- In Swagger, click **Authorize** (lock icon) and paste:

    ```
    Bearer <your_token_here>
    ```

- Authenticated endpoints will now work from Swagger UI.

---

## Middleware

### ErrorHandlingMiddleware

- Catches all unhandled exceptions and returns a structured ProblemDetails response with a correlation ID for debugging.

### RequestResponseLoggingMiddleware

- Logs incoming requests and outgoing responses, including:
  - Execution time in milliseconds.
  - Endpoint invocation count.
  - Request/response bodies for debugging.

### AuthenticationMiddleware

- Custom JWT authentication:
  - Validates token signature and expiration.
  - Extracts claims and attaches to `HttpContext.User`.
  - Returns `401 Unauthorized` for invalid/missing tokens.

---

## Example Token Request

```bash
curl -X POST "http://localhost:5069/login?username=admin&password=1234"
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI..."
}
```

---

## Example Authenticated Request

```bash
curl -X GET "http://localhost:5069/users" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI..."
```

---

## AI Contribution

The development process involved AI assistance in:

- Generating boilerplate code for Minimal API setup with EF Core.
- Designing validation rules for DTOs using FluentValidation.
- Implementing middlewares for logging, error handling, and authentication.
- Creating Swagger/OpenAPI examples and adding detailed documentation for endpoints.
- Debugging complex issues, such as:
    - Handling ProblemDetails for different status codes.
    - Fixing `IDX10720` key-size error by switching to a 256-bit JWT signing key.
    - Avoiding `ObjectDisposedException` in request logging middleware by correctly handling response streams.
- Improving code structure by extracting endpoints and validators into dedicated classes.

> The AI was used as a collaborative assistant — suggesting implementation strategies, providing optimized code snippets, and ensuring the application adhered to best practices in .NET API development.

---
