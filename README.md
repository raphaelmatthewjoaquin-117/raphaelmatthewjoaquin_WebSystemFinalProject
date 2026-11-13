# Student Grade Management

ASP.NET Core MVC and Web API application for faculty grade management and student-owned grade viewing. The app uses ASP.NET Core Identity, EF Core, and Supabase PostgreSQL. Faculty and student dashboards consume the protected API with Fetch; the API calculates every remark in C#.

## Requirements

- .NET 10 SDK
- A Supabase PostgreSQL project (or another PostgreSQL 14+ server)
- Postman for API/security demonstrations
- For deployment: a Render account and a Git repository containing this project

## Database and local configuration

Create a Supabase project and copy its PostgreSQL connection details. Use the **session pooler** or direct connection rather than the transaction pooler because the application runs EF Core migrations at startup. In the application project, use .NET user-secrets for local-only configuration:

```powershell
cd StudentGradeManagement
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Authentication:Jwt:Key" "REPLACE_WITH_A_RANDOM_SECRET_AT_LEAST_32_BYTES_LONG"
dotnet user-secrets set "Seed:Faculty:Email" "faculty@example.test"
dotnet user-secrets set "Seed:Faculty:Password" "A-unique-demo-password-123!"
dotnet user-secrets set "Seed:Faculty:FacultyNumber" "FAC-001"
dotnet user-secrets set "Seed:Student:Email" "student@example.test"
dotnet user-secrets set "Seed:Student:Password" "A-different-demo-password-123!"
dotnet user-secrets set "Seed:Student:StudentNumber" "2026-0001"
```

Replace the example values locally; do not commit real passwords, JWT keys, or connection strings. Passwords are processed and hashed by ASP.NET Core Identity and are never stored in the academic tables. Seed credentials are for demonstrations only.

EF Core migrations are under `StudentGradeManagement/Data/Migrations`. To apply them manually:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef database update
```

The application also applies pending migrations at startup. Its idempotent seed routine independently fills missing sample student/faculty records and sample grades; it creates the configured demonstration accounts only when the matching seed settings are present. The database enforces grade range `0..100`, foreign keys, unique student/faculty numbers, and one grade per student/subject/semester/school year.

## Run locally

From the workspace root:

```powershell
dotnet run --project .\StudentGradeManagement\StudentGradeManagement.csproj
```

Open the HTTPS or HTTP URL printed by ASP.NET Core. Sign in with one of the configured demonstration accounts. Faculty accounts land on the grade-management dashboard; student accounts can open only their own grade dashboard. No public registration link is shown; accounts are provisioned by Identity seed configuration and academic-record mappings.

## API and authorization

Obtain a short-lived (30-minute) JWT for Postman:

```http
POST /api/auth/token
Content-Type: application/json

{"email":"faculty@example.test","password":"<configured faculty demo password>"}
```

Send the returned `accessToken` as `Authorization: Bearer <token>`.

| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/auth/token` | Public credential exchange |
| GET | `/api/grades` | Faculty |
| GET | `/api/grades/students` | Faculty |
| GET | `/api/grades/{id}` | Faculty |
| POST | `/api/grades` | Faculty |
| PUT | `/api/grades/{id}` | Faculty |
| DELETE | `/api/grades/{id}` | Faculty |
| GET | `/api/student/grades` | Student, filtered by the signed-in account mapping |

Grade writes accept `studentId`, `subjectCode`, `subjectName`, whole-number `grade`, `semester`, and `schoolYear`. `facultyId` and `remarks` are not accepted as authoritative inputs: the API obtains the faculty mapping from the authenticated account and calculates remarks itself. Grades must be integers from 0 to 100. Duplicate final grades return `409 Conflict`. Student API access has no caller-selectable student ID; an extra query string does not change the account-derived filter.

The MVC dashboards use the signed-in Identity session and a short-lived API token to call the same API. The student page contains no grade write controls. The API independently enforces JWT authentication, roles, active academic mappings, and student ownership.

## Postman verification

Import `postman/StudentGradeManagement.postman_collection.json`. Set the collection variables `baseUrl`, `facultyEmail`, `facultyPassword`, `studentEmail`, `studentPassword`, and `expectedStudentNumber`; use the demo values configured above. Run the collection requests to capture:

- faculty list/create/update/delete, including a false client `remarks` value and the returned computed remark;
- grades `105`, `-1`, and `84.5`, which should return `400`;
- the student's own grades and a request with an attempted `studentId` query-string override;
- student `GET` of faculty data and `POST`/`PUT`/`DELETE` attempts, which should return `403`;
- an unauthenticated faculty endpoint, which should return `401`.

The collection has response assertions and stores issued tokens and created record identifiers in collection variables. For the examination submission, run it against the deployed URL, retain the Postman request/response evidence (screenshots or exported run), and be sure to show both successful and rejected cases. This repository does not contain fabricated screenshots or live credentials.

## Tests

```powershell
dotnet test .\StudentGradeManagement.Tests\StudentGradeManagement.Tests.csproj
dotnet build .\StudentGradeManagement\StudentGradeManagement.csproj
```

The unit tests cover every remark boundary, out-of-range values, missing grades, and fractional JSON grades.

## Deploy to Render + Supabase

1. Push the repository to GitHub and create a Supabase PostgreSQL project. Copy the session-pooler/direct database connection string and keep its password private.
2. In Render, create a Blueprint from the repository using `render.yaml`. The Blueprint requests the database connection string and demo account credentials; set these as Render environment secrets. It generates a JWT signing secret.
3. Use a strong, unique faculty and student demo password that satisfies Identity's password policy. The sample academic rows include `FAC-001` and `2026-0001`.
4. Deploy the Docker web service. On startup it applies EF migrations, inserts missing sample academic data, provisions the configured Identity roles/accounts, and maps those accounts to the selected academic records.
5. Confirm the deployment's `/health` endpoint responds with `200`, sign in to both dashboards, then run the Postman collection against the Render URL.

The Render service is configured to bind to Render's `PORT`. Do not add credentials to `render.yaml`, `appsettings.json`, or source control. Use one application instance during initial migration/deployment; review migration strategy before scaling out multiple instances.

## Solution layout

- `StudentGradeManagement/Controllers/Api`: authentication and REST endpoints.
- `StudentGradeManagement/Controllers`: authenticated MVC dashboards.
- `StudentGradeManagement/Data`: PostgreSQL EF Core context and migrations.
- `StudentGradeManagement/Models`: Identity mappings, academic entities, DTOs, and server-side remark calculation.
- `StudentGradeManagement/wwwroot/js`: Fetch-based dashboard behavior.
- `StudentGradeManagement.Tests`: automated validation and grade-scale tests.
