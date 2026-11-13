# Student Grade Management

This project is an ASP.NET Core MVC + Web API application for managing grades in a faculty/student workflow. The same web app hosts:

- MVC pages for faculty and student dashboards
- JWT-protected API endpoints
- ASP.NET Core Identity for authentication and role-based access
- EF Core + PostgreSQL persistence

The API calculates grade remarks on the server and enforces role-based authorization.

## Prerequisites

Before you begin, make sure you have:

- .NET 10 SDK
- PostgreSQL 14+ (local PostgreSQL, Supabase, or another managed PostgreSQL instance)
- Postman or curl for API testing
- Optional: a GitHub repo and Render deployment account for hosting

## Database setup

1. Create a PostgreSQL database.
2. Use the database connection string from your PostgreSQL provider.
3. For Supabase, use the direct connection or session pooler connection, not the transaction pooler; the app applies EF Core migrations at startup.

Example local PostgreSQL connection string:

```text
Host=localhost;Port=5432;Database=student_grade_management;Username=postgres;Password=your_password;SSL Mode=Disable
```

Example Supabase connection string:

```text
Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.your_project_ref;Password=your_password;SSL Mode=Require;Trust Server Certificate=true
```

## Configuration

Store local-only secrets with .NET user secrets so they are not committed to source control.

From the project root:

```powershell
dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=student_grade_management;Username=postgres;Password=your_password;SSL Mode=Disable"
dotnet user-secrets set "Authentication:Jwt:Key" "ReplaceThisWithASecretAtLeast32BytesLong"
dotnet user-secrets set "Authentication:Jwt:Issuer" "StudentGradeManagement"
dotnet user-secrets set "Authentication:Jwt:Audience" "StudentGradeManagement"

dotnet user-secrets set "Seed:Faculty:Email" "faculty@example.test"
dotnet user-secrets set "Seed:Faculty:Password" "A-unique-demo-password-123!"
dotnet user-secrets set "Seed:Faculty:FacultyNumber" "FAC-001"

dotnet user-secrets set "Seed:Student:Email" "student@example.test"
dotnet user-secrets set "Seed:Student:Password" "A-different-demo-password-123!"
dotnet user-secrets set "Seed:Student:StudentNumber" "2026-0001"
```

Notes:

- Replace the example values with your own strong secret values.
- Do not commit real connection strings, JWT keys, or passwords.
- The database is seeded in a secure way; user passwords are stored as ASP.NET Core Identity hashes, not in academic tables.
- The application expects both `Seed:Faculty:*` and `Seed:Student:*` values to be configured together for each role.

## Migration and seed instructions

The app uses EF Core Migrations and runs them automatically on startup:

```powershell
dotnet ef database update --project .\StudentGradeManagement\StudentGradeManagement.csproj
```

If `dotnet-ef` is not installed yet:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
```

The startup pipeline runs:

- `Database.MigrateAsync()` to apply migrations
- `DatabaseSeeder.SeedAsync(...)` to ensure required roles exist, seed sample academic records, and create the configured demo accounts

The seed routine creates:

- sample students and faculty members
- sample grade entries
- faculty and student demo accounts using the `Seed:*` secrets

The database enforces constraints such as:

- grade range 0 to 100
- unique student and faculty numbers
- foreign keys between grades and academic records
- one grade per student, subject, semester, and school year

## Run the API and MVC app

From the repository root:

```powershell
dotnet restore

dotnet build .\StudentGradeManagement\StudentGradeManagement.csproj

dotnet run --project .\StudentGradeManagement\StudentGradeManagement.csproj
```

The app will print a local URL such as:

- `https://localhost:5001`
- or `http://localhost:5000`

Open the printed URL in a browser.

The same project serves both the MVC views and the REST API:

- MVC login page: `/Identity/Account/Login`
- Faculty dashboard: `/FacultyDashboard`
- Student dashboard: `/StudentDashboard`
- Health check: `/health`

## Testing Faculty and Student accounts

Use the configured seeded demo accounts to verify both roles.

### Faculty demo account

Set these values in user secrets or use your own demo values:

```text
Seed:Faculty:Email = faculty@example.test
Seed:Faculty:Password = A-unique-demo-password-123!
Seed:Faculty:FacultyNumber = FAC-001
```

Login at `/Identity/Account/Login` using the faculty email and password.

After login, the faculty dashboard should load and allow grade-management tasks.

API token test:

```http
POST /api/auth/token
Content-Type: application/json

{
  "email": "faculty@example.test",
  "password": "A-unique-demo-password-123!"
}
```

Use the returned `accessToken` in the `Authorization: Bearer <token>` header for protected API requests.

### Student demo account

Set these values in user secrets or use your own demo values:

```text
Seed:Student:Email = student@example.test
Seed:Student:Password = A-different-demo-password-123!
Seed:Student:StudentNumber = 2026-0001
```

Login at `/Identity/Account/Login` using the student email and password.

After login, the student dashboard should load and display only the signed-in student’s grades.

The student is forbidden from accessing faculty endpoints and cannot modify grades through the student API.

### Postman verification

Import the collection file in `postman/StudentGradeManagement.postman_collection.json`.

Set the collection variables:

- `baseUrl`
- `facultyEmail`
- `facultyPassword`
- `studentEmail`
- `studentPassword`
- `expectedStudentNumber`

Run the requests to confirm:

- faculty can list/create/update/delete grades
- invalid grade values like `105`, `-1`, and `84.5` are rejected with `400`
- student can access only their own grade data
- student attempts to call faculty endpoints return `403`
- unauthenticated access to faculty API endpoints returns `401`

## API authorization summary

The app exposes the following main endpoints:

| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/auth/token` | Public login exchange |
| GET | `/api/grades` | Faculty |
| GET | `/api/grades/students` | Faculty |
| GET | `/api/grades/{id}` | Faculty |
| POST | `/api/grades` | Faculty |
| PUT | `/api/grades/{id}` | Faculty |
| DELETE | `/api/grades/{id}` | Faculty |
| GET | `/api/student/grades` | Student |

The API validates JWTs, role membership, and mapped academic records. Student requests are filtered by the signed-in account, and a caller cannot override the student context by passing another `studentId` in the query string.

## Testing

Run the automated tests:

```powershell
dotnet test .\StudentGradeManagement.Tests\StudentGradeManagement.Tests.csproj
```

Run a build to confirm the app compiles:

```powershell
dotnet build .\StudentGradeManagement\StudentGradeManagement.csproj
```

The test project covers grade boundaries, invalid values, missing grade cases, and fractional JSON grade validation.

## Deployment notes

For Render + Supabase:

1. Push the code to GitHub.
2. Create a Supabase PostgreSQL project.
3. Copy the direct or session pooler connection string and store it as a secure environment variable.
4. In Render, configure the app using `render.yaml` and set the required environment variables for database connection and demo account credentials.
5. Confirm the app is running and the `/health` endpoint returns `200`.

Do not commit production secrets to source control. Use a single app instance during initial deployment and review your migration strategy before scaling horizontally.

## Solution layout

- `StudentGradeManagement/Controllers`: MVC controllers and dashboard pages
- `StudentGradeManagement/Controllers/Api`: API controllers and auth endpoints
- `StudentGradeManagement/Data`: EF Core DbContext and migrations
- `StudentGradeManagement/Models`: domain models, DTOs, and grade remark logic
- `StudentGradeManagement/Services`: database seeding and token generation
- `StudentGradeManagement/wwwroot/js`: Fetch-based client behavior
- `StudentGradeManagement.Tests`: validation and grading tests
- `postman/StudentGradeManagement.postman_collection.json`: API verification collection
