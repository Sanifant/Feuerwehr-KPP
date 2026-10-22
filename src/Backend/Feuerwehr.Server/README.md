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

# SMTP email delivery

The API sends invitation and system emails through SMTP using MailKit. Configure
the API process/container with environment variables; no provider credentials
belong in appsettings files or source control.

| Environment variable | Meaning |
| --- | --- |
| `MAIL_HOST` | SMTP hostname, e.g. `smtp.strato.de`. Overrides the Aspire mail connection. |
| `MAIL_PORT` | SMTP port, from 1 to 65535. Defaults to 465 for `SslOnConnect`, 587 for `StartTls`, or 1025 for `None`. |
| `MAIL_SECURITY` | `SslOnConnect` for immediate TLS, `StartTls` for required STARTTLS, or `None` for local Mailpit. An explicitly configured provider defaults to immediate TLS on port 465, otherwise required STARTTLS. There is no automatic fallback to plaintext. |
| `MAIL_USERNAME` | SMTP account name; for Strato, the complete mailbox email address. |
| `MAIL_PASSWORD` | Mailbox password. Set together with `MAIL_USERNAME`; authentication requires TLS. |
| `MAIL_FROM` | Sender email address, normally the authenticated mailbox. Defaults to `Mail__From` for compatibility, then `MAIL_USERNAME`, then `noreply@localhost.de`. |
| `MAIL_FROM_NAME` | Optional sender display name. |
| `MAIL_TIMEOUT_SECONDS` | SMTP network operation timeout in seconds; defaults to 30. |

For Strato hosting mailboxes, use the following environment configuration with
your actual mailbox and password. Strato documents SMTP on port 465 with SSL/TLS
in its [server settings](https://www.strato.de/faq/mail/e-mailserver-adressen-ports-ssl-tls/):

```dotenv
MAIL_HOST=smtp.strato.de
MAIL_PORT=465
MAIL_SECURITY=SslOnConnect
MAIL_USERNAME=noreply@example.org
MAIL_PASSWORD=<mailbox-password>
MAIL_FROM=noreply@example.org
MAIL_FROM_NAME=Feuerwehr
MAIL_TIMEOUT_SECONDS=30
Frontend__BaseUrl=https://feuerwehr.example.org
```

`Frontend__BaseUrl` controls the destination of password invitation links, not
the SMTP server. Configure it to the externally reachable frontend URL.

A provider offering STARTTLS instead can use its hostname, port 587 and
`MAIL_SECURITY=StartTls`. Certificate validation remains enabled.

For local debugging, start the AppHost with `aspire start --apphost
Feuerwehr.AppHost/Feuerwehr.AppHost.csproj`. In Aspire run mode (including Debug),
the AppHost adds Mailpit, waits for it, and overrides inherited provider settings
with unauthenticated local SMTP. Emails appear in Mailpit's web UI, accessible
from the Aspire dashboard. SMTP host and port come from `ConnectionStrings__mail`,
which Aspire provides automatically. A standalone local API can use `MAIL_HOST=localhost`,
`MAIL_PORT=1025`, and `MAIL_SECURITY=None` with no credentials.

When publishing through Aspire, Mailpit is omitted. The AppHost forwards the
provider variables as external parameters, marking `mail-password` as secret.
Supply `MAIL_HOST`, `MAIL_PORT`, `MAIL_USERNAME`, `MAIL_PASSWORD` and `MAIL_FROM`
to the publishing environment (or resolve the corresponding Aspire parameters).
`MAIL_SECURITY`, `MAIL_FROM_NAME` and `MAIL_TIMEOUT_SECONDS` default to `StartTls`,
`Feuerwehr` and `30` respectively and can also be supplied via environment.

The repository's `deployment/docker-compose.yml` forwards these variables to the
API container. The SMTP section in `deployment/.env.examples` contains matching
Strato placeholders. After changing your deployment's `.env`, recreate the API
container so it receives the updated environment.
