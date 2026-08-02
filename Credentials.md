Verifying the seeded user/role setup and current connection string before starting the backend.

Read [](file:///e%3A/Projects/GitHub/ftc-perfomance/appsettings.json)

Read [](file:///e%3A/Projects/GitHub/ftc-perfomance/Infrastructure/Persistence/Seed/DbInitializer.cs#1-1), lines 1 to 320

Checking seeded user definitions and roles so I can provide exact credentials and then start the backend.

Read [](file:///e%3A/Projects/GitHub/ftc-perfomance/Infrastructure/Persistence/Seed/DbInitializer.cs#320-320), lines 320 to 700

Gathering seeded user data and role mappings so I can provide exact credentials before starting the backend.

Searched for regex `GetDemoUserSeeds|new\(.*Role|DemoUserSeed|DemoUserResponse|SecurityModel\.` (`**/Infrastructure/Persistence/Seed/DbInitializer.cs`), 66 results

Starting the backend with the remote DB and gathering startup confirmation.

Ran terminal command: dotnet run --project FTCERP.Host.csproj

Read [](file:///c%3A/Users/Freedom/AppData/Roaming/Code/User/workspaceStorage/fc01d4dd4b6fb9d2ea3e140d7c0e400d/GitHub.copilot-chat/chat-session-resources/df7fca5b-0567-4067-99e9-7517027d5bdf/call_lHw5hhhd5ZCwXz4V2oQedKpZ__vscode-1785661321518/content.txt#1-1), lines 1 to 120

Confirming the backend startup and collecting the exact seeded demo user credentials.

Checked terminal output

Collecting the exact demo user list for each seeded role so I can report credentials precisely.

Read [](file:///e%3A/Projects/GitHub/ftc-perfomance/Infrastructure/Persistence/Seed/DbInitializer.cs#720-720), lines 720 to 800

The backend is running against the remote database.

## Remote backend status
- Started successfully
- Listening on: `http://localhost:5000`
- Using DB: `ftcperfomancetrack`
- Remote SQL server: `AH-EPYC-3-SQL2019.zadns.co.za`

## Seeded credentials

All seeded demo users use the same default password:

- `P@ssw0rd123!`

### SuperAdmin
- `lethabo.khumalo` / `P@ssw0rd123!`
- `annelie.bothma` / `P@ssw0rd123!`
- `sipho.nene` / `P@ssw0rd123!`

### Admin
- `thandiwe.maseko` / `P@ssw0rd123!`
- `kabelo.mahlangu` / `P@ssw0rd123!`
- `melissa.jacobs` / `P@ssw0rd123!`

### ClientAdmin
- `koketso.molefe` / `P@ssw0rd123!`
- `johan.steyn` / `P@ssw0rd123!`
- `nosipho.mbatha` / `P@ssw0rd123!`

### AuditorGeneral
- `maria.fernandez` / `P@ssw0rd123!`
- `bongani.mabena` / `P@ssw0rd123!`
- `tracey.adams` / `P@ssw0rd123!`

### PmsPerformanceManager
- `mandla.dube` / `P@ssw0rd123!`
- `zanele.mokoena` / `P@ssw0rd123!`
- `gerhard.swanepoel` / `P@ssw0rd123!`

### InternalAudit
- `azwi.netshifhefhe` / `P@ssw0rd123!`
- `riaan.pretorius` / `P@ssw0rd123!`
- `fatima.ismail` / `P@ssw0rd123!`

### Reviewer
- `nomvula.mkhize` / `P@ssw0rd123!`
- `francois.duplessis` / `P@ssw0rd123!`
- `boitumelo.radebe` / `P@ssw0rd123!`

### Approver
- `priya.naidoo` / `P@ssw0rd123!`
- `themba.zulu` / `P@ssw0rd123!`
- `megan.vanwyk` / `P@ssw0rd123!`
- `thabo.mokoena` / `P@ssw0rd123!`
- `lindiwe.mahlangu` / `P@ssw0rd123!`
- `ahmed.hassan` / `P@ssw0rd123!`
- `nomsa.dlamini` / `P@ssw0rd123!`
- `keitumetse.mabaso` / `P@ssw0rd123!`
- `bradley.peters` / `P@ssw0rd123!`

### Verifier
- `ayabonga.cele` / `P@ssw0rd123!`
- `reneilwe.mogale` / `P@ssw0rd123!`
- `willem.nel` / `P@ssw0rd123!`

### Submitter
- `mpho.madonsela` / `P@ssw0rd123!`
- `helena.visagie` / `P@ssw0rd123!`
- `sizwe.nxumalo` / `P@ssw0rd123!`

> Note: The built-in system admin user is seeded from appsettings.json:
> - `superadmin@example.com` / `P@ssw0rd123!`

If you want, I can also give you the exact endpoints for login and Swagger UI.