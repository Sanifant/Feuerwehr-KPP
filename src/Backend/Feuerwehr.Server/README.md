# Initial administrator

`DbInitializer` seeds roles, the initial administrator account, and default diary
categories during API startup, after database migrations.

Before the first startup, supply the initial administrator password through the
`SeedAdmin__Password` environment variable. ASP.NET Core maps this variable to
the `SeedAdmin:Password` configuration key. There is no default password.

Set `SeedAdmin__Email` (`SeedAdmin:Email`) to choose the administrator's email
and username. If omitted or blank, it defaults to `admin@feuerwehr.local`.
Leading and trailing whitespace is removed. Identity validates the email when
creating the account.

From `src/` in PowerShell, with the API's database and Redis connections configured:

```powershell
$env:SeedAdmin__Email = 'admin@example.org'
$env:SeedAdmin__Password = '<your-initial-admin-password>'
dotnet run --project Backend/Feuerwehr.Server/Feuerwehr.Server.csproj
```

Replace the placeholder with a private password of at least eight characters,
including uppercase, lowercase, and a digit (the current Identity policy).
Inject the value through your deployment's secret/environment configuration;
do not commit it to configuration files or launch profiles.

The Aspire AppHost reads the same variables and forwards them to the API using
the `seed-admin-email` parameter and the secret parameter `seed-admin-password`:

```powershell
$env:SeedAdmin__Email = 'admin@example.org'
$env:SeedAdmin__Password = '<your-initial-admin-password>'
aspire start --apphost Feuerwehr.AppHost/Feuerwehr.AppHost.csproj
```

Aspire requires its secret parameter to be resolved when starting the server
resource or deploying. Supply it through AppHost configuration or the parameter
prompt; deployed API containers receive it as `SeedAdmin__Password`.

When the admin account does not exist, a missing, blank, or Identity-invalid
password fails startup. Creation and role-assignment failures also fail startup.
Passwords are never included in the initializer's logs or exception messages.

When an account with the configured email already exists, the initializer preserves its password and
ensures its `Admin`, `IncidentDiaryEditor`, and `SituationMapEditor` roles.
The API itself no longer requires the seed password in this case. Changing the
environment variable does not reset an existing account's password; use the
authenticated password-change flow for that.

Keep the email setting consistent across restarts. Changing it selects a different
account to seed and does not rename the previously seeded administrator.

Earlier authentication guides that show a fixed default admin password are
outdated. Existing installations keep their current password until it is changed.
