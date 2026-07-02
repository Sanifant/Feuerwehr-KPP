# Feuerwehr-KPP Authentication System Documentation

## Overview

This project implements a comprehensive JWT-based authentication system with role-based authorization across three platforms:
- **Backend API** (ASP.NET Core with Identity)
- **React Frontend** (TypeScript/Vite)
- **Mobile App** (Avalonia - foundation ready)

## Architecture

### Backend (ASP.NET Core Identity + JWT)

#### Core Components

1. **ApplicationUser** (`Models/ApplicationUser.cs`)
   - Extends `IdentityUser` with custom fields
   - Properties: `FirstName`, `LastName`, `FireDepartmentId`, `CreatedAt`, `LastLoginAt`, `IsActive`
   - Navigation: `FireDepartment`, computed `FullName`

2. **TokenService** (`Services/TokenService.cs`)
   - Generates JWT access tokens (configurable expiration, default 30min)
   - Generates secure refresh tokens (configurable expiration, default 7 days)
   - Stores hashed refresh tokens using Identity token storage
   - Validates expired access tokens for refresh flow
   - Token rotation on refresh (old token invalidated)

3. **Auth Controllers**
   - **AuthController**: `POST /api/Auth/login`, `/refresh`, `/logout`, `GET /me`
   - **UsersController**: Admin-only CRUD for user management

4. **Authorization Policies** (`Authorization/Policies.cs`)
   - `RequireAdmin`: Admin role only
   - `RequireCommander`: Admin or Commander
   - `CanViewData`: All authenticated users
   - `CanManageHydrants`: Admin, Commander, Firefighter
   - `CanManageTraining`: Admin, Commander
   - `CanManageUsers`: Admin only

#### Roles

| Role | Level | Permissions |
|------|-------|-------------|
| **Admin** | 4 | Full system access, user management |
| **Commander** | 3 | Manage training, fire departments, view all data |
| **Firefighter** | 2 | Manage hydrants, view data |
| **Viewer** | 1 | View-only access to data |

#### Configuration

**appsettings.json**:
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars-change-for-production!",
    "Issuer": "FeuerwehrKPP",
    "Audience": "FeuerwehrKPP",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  }
}
```

**⚠️ PRODUCTION SECURITY**:
- Move `SecretKey` to environment variables or Azure Key Vault
- Use at least 256-bit (32+ character) secret
- Enable HTTPS only
- Consider rate limiting on auth endpoints

#### Database Seeding

**Default Admin Account** (`Data/DbInitializer.cs`):
- Email: `admin@feuerwehr.local`
- Password: `Admin@123456`
- **⚠️ Change this password immediately after first login!**

### Frontend (React + TypeScript)

#### Core Components

1. **AuthContext** (`src/AuthContext.tsx`)
   - Manages authentication state
   - Decodes JWT to extract user info and roles
   - Stores tokens in localStorage
   - Helper: `hasRole(role: string)`

2. **API Client** (`src/api/apiClient.ts`)
   - Axios instance with Bearer token injection
   - Automatic token refresh on 401
   - Request queuing during refresh
   - Logout and redirect on refresh failure

3. **Role Helpers**
   - `useRole()` hook: `isAdmin`, `isCommander`, `isFirefighter`, `isViewer`, permission flags
   - `<RoleGuard roles={['Admin', 'Commander']}>`: Conditional rendering

4. **Protected Routes**
   - `<ProtectedRoute>`: Redirects to `/login` if not authenticated
   - Role-aware navigation in `SideBar.tsx`
   - User info display in `Navbar.tsx`

5. **User Management** (`Components/UserManagement.tsx`)
   - Admin-only page at `/users`
   - List  all users with roles and status
   - Edit/delete user functionality (backend ready)

#### Frontend Auth Flow

```
1. User enters credentials → POST /api/Auth/login
2. Store access + refresh tokens in localStorage
3. Decode JWT to extract: userId, email, name, roles, departmentId
4. Set auth state → redirect to dashboard
5. API calls inject Bearer token automatically
6. On 401 → try refresh → retry request OR logout
```

### Mobile App (Avalonia)

#### Authentication Foundation (Ready to Use)

1. **IAuthService** (`Services/IAuthService.cs`)
   - `LoginAsync(email, password)`: Authenticate user
   - `LogoutAsync()`: Clear tokens, notify server
   - `RefreshTokenAsync()`: Get new access token
   - `GetAccessTokenAsync()`: Auto-refresh if expired
   - Properties: `IsAuthenticated`, `UserEmail`, `UserFullName`, `UserRoles`
   - Event: `AuthStateChanged`

2. **AuthService** (`Services/AuthService.cs`)
   - JWT decoding with `System.IdentityModel.Tokens.Jwt`
   - Automatic token refresh when near expiration
   - Loads stored tokens on app startup

3. **ISecureStorage** + **InMemorySecureStorage**
   - Abstract secure token storage
   - Current: In-memory (for development/desktop)
   - **TODO for Production**:
     - Android: Use Xamarin.Essentials.SecureStorage or Android KeyStore
     - iOS: Use Keychain
     - Windows: Use Data Protection API (DPAPI)

4. **AuthenticatedHttpClient** (`Services/AuthenticatedHttpClient.cs`)
   - Wrapper around HttpClient
   - Automatically injects Bearer tokens
   - Ready to replace direct HttpClient usage in services

#### Mobile Implementation Steps (TODO)

1. **Create Login UI**:
   - LoginView.axaml (email/password form)
   - LoginViewModel (MVVM pattern with CommunityToolkit.Mvvm)

2. **Update App.axaml.cs**:
   - Check `IAuthService.IsAuthenticated` on startup
   - Show LoginView if not authenticated
   - Show MainWindow if authenticated

3. **Platform-Specific Secure Storage**:
   - Android: Implement `ISecureStorage` using `Android.Security.Keystore`
   - iOS: Implement using `Security.SecKeyChain`
   - Register platform-specific implementation in DI

4. **Update Existing Services**:
   - Replace `HttpClient` with `AuthenticatedHttpClient` in:
     - `HydrantService.cs`
     - Any new API services

## Usage Examples

### Backend: Protect an Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.CanViewData)] // All authenticated users
public class MyController : ControllerBase
{
    [HttpGet]
    public IActionResult GetData() { ... }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAdmin)] // Admin only
    public IActionResult CreateData() { ... }
}
```

### Frontend: Role-Based UI

```tsx
import { useRole } from './hooks/useRole';
import RoleGuard from './components/RoleGuard';

function MyComponent() {
    const { isAdmin, canManageHydrants } = useRole();

    return (
        <div>
            {canManageHydrants && (
                <button>Add Hydrant</button>
            )}

            <RoleGuard roles={['Admin', 'Commander']}>
                <button>Advanced Settings</button>
            </RoleGuard>
        </div>
    );
}
```

### Mobile: Using Auth Service

```csharp
public class MyViewModel
{
    private readonly IAuthService _authService;
    private readonly AuthenticatedHttpClient _httpClient;

    public async Task LoginAsync(string email, string password)
    {
        var success = await _authService.LoginAsync(email, password);
        if (success)
        {
            // Navigate to main view
            // User info available: _authService.UserFullName, UserRoles
        }
    }

    public async Task LoadDataAsync()
    {
        // AuthenticatedHttpClient automatically adds Bearer token
        var response = await _httpClient.GetAsync("/api/Hydrant");
        // ...
    }
}
```

## Security Best Practices

### ✅ Implemented
- JWT with configurable expiration
- Refresh token rotation
- Hashed refresh tokens in database
- Role-based authorization policies
- HTTPS enforcement ready
- Password complexity requirements (Identity defaults)
- User account lockout (5 failed attempts)

### ⚠️ TODO for Production
1. **JWT Secret Management**:
   ```bash
   # Use environment variables
   export JwtSettings__SecretKey="your-production-secret-min-32-chars"

   # Or Azure Key Vault
   az keyvault secret set --vault-name MyVault --name JwtSecret --value "..."
   ```

2. **Rate Limiting** (add to Program.cs):
   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.AddFixedWindowLimiter("auth", o =>
       {
           o.Window = TimeSpan.FromMinutes(1);
           o.PermitLimit = 5;
       });
   });

   // Then on AuthController:
   [EnableRateLimiting("auth")]
   ```

3. **CORS** (already configured for development, review for production)

4. **HTTPS Redirect** (enable in Program.cs):
   ```csharp
   app.UseHttpsRedirection();
   ```

5. **Two-Factor Authentication** (future enhancement):
   ```csharp
   await _userManager.SetTwoFactorEnabledAsync(user, true);
   ```

## Testing the System

### 1. Test Login (Backend)
```bash
curl -X POST https://localhost:7538/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@feuerwehr.local","password":"Admin@123456"}'
```

### 2. Test Protected Endpoint
```bash
curl -X GET https://localhost:7538/api/Users \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### 3. Test Token Refresh
```bash
curl -X POST https://localhost:7538/api/Auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"YOUR_REFRESH_TOKEN"}'
```

### 4. Frontend
1. Navigate to `http://localhost:5173/login`
2. Login with: `admin@feuerwehr.local` / `Admin@123456`
3. Check browser DevTools → Application → Local Storage for tokens
4. Navigate to `/users` (Admin only)

## Adding New Roles/Policies

1. **Add Role Constant** (`Authorization/Policies.cs`):
   ```csharp
   public static class Roles
   {
       // ...
       public const string MyNewRole = "MyNewRole";
   }
   ```

2. **Add Policy** (`Authorization/Policies.cs`):
   ```csharp
   public static class PolicyNames
   {
       // ...
       public const string CanDoSomething = "CanDoSomething";
   }

   public static void AddFeuerwehrPolicies(this AuthorizationOptions options)
   {
       // ...
       options.AddPolicy(PolicyNames.CanDoSomething, policy =>
           policy.RequireRole(Roles.Admin, Roles.MyNewRole));
   }
   ```

3. **Seed the Role** (`Data/DbInitializer.cs`):
   ```csharp
   await roleManager.CreateAsync(new IdentityRole(Roles.MyNewRole));
   ```

4. **Use in Controller**:
   ```csharp
   [Authorize(Policy = PolicyNames.CanDoSomething)]
   public IActionResult MyAction() { ... }
   ```

5. **Update Frontend** (`hooks/useRole.ts`):
   ```typescript
   export const useRole = () => {
       // ...
       const isMyNewRole = hasRole('MyNewRole');
       const canDoSomething = hasRole('Admin') || hasRole('MyNewRole');
       return { ..., isMyNewRole, canDoSomething };
   };
   ```

## Troubleshooting

### "Unauthorized 401" on API calls
- Check token in localStorage: `localStorage.getItem('access_token')`
- Verify token is not expired: decode at jwt.io
- Check Authorization header in browser Network tab
- Verify backend JWT settings match (Issuer, Audience)

### Refresh token fails
- Refresh token expired (default 7 days)
- User logged out / token revoked
- Database refresh token deleted
- Solution: User must log in again

### Role check fails frontend
- JWT doesn't contain role claims → check backend `TokenService`
- Role name mismatch (case-sensitive)
- Token not decoded properly → check `AuthContext`

### Mobile app won't build
- Missing System usings → all auth files now have explicit usings
- Package version mismatch → check `Directory.Packages.props`
- Android SDK issues → update Android SDK tools

## Files Changed/Created

### Backend
- ✅ `Models/ApplicationUser.cs` - Custom Identity user
- ✅ `Services/ITokenService.cs` + `TokenService.cs` - JWT generation
- ✅ `Controller/AuthController.cs` - Login/refresh/logout endpoints
- ✅ `Controller/UsersController.cs` - User management (admin)
- ✅ `Authorization/Policies.cs` - Role/policy definitions
- ✅ `Data/FeuerwehrDbContext.cs` - Changed to `IdentityDbContext<ApplicationUser>`
- ✅ `Data/DbInitializer.cs` - Role/admin seeding
- ✅ `Program.cs` - Identity, JWT, policies, seeding
- ✅ `appsettings.json` - JWT configuration
- ✅ Migration: `AddIdentityTables`

### Frontend
- ✅ `src/AuthContext.tsx` - Auth state with real API calls
- ✅ `src/api/apiClient.ts` - Axios with Bearer + refresh
- ✅ `src/hooks/useRole.ts` - Role flags
- ✅ `src/components/RoleGuard.tsx` - Conditional rendering
- ✅ `src/Components/UserManagement.tsx` - Admin user list
- ✅ `src/Navbar.tsx` - User display + logout
- ✅ `src/SideBar.tsx` - Role-based navigation
- ✅ `src/App.tsx` - Added `/users` route

### Mobile
- ✅ `Services/IAuthService.cs` + `AuthService.cs` - JWT auth
- ✅ `Services/ISecureStorage.cs` + `InMemorySecureStorage.cs` - Token storage
- ✅ `Services/AuthenticatedHttpClient.cs` - HTTP wrapper
- ✅ `Feuerwehr.App.csproj` - Added JWT package
- ✅ `Directory.Packages.props` - JWT version
- ✅ `App.axaml.cs` - Registered auth services

### Shared
- ✅ `Feuerwehr.Common/Models/Auth/` - LoginRequest, LoginResponse, RefreshTokenRequest, RegisterUserRequest

## Next Steps

1. **Change default admin password** immediately
2. **Move JWT secret** to Azure Key Vault or environment variable
3. **Implement mobile login UI** (LoginView.axaml + ViewModel)
4. **Add rate limiting** to auth endpoints
5. **Implement platform-specific secure storage** for mobile
6. **Add user profile editing** (change password, update info)
7. **Add password reset flow** (email-based)
8. **Consider 2FA** for admin accounts
9. **Add audit logging** for sensitive operations
10. **Write integration tests** for auth flows

## Support

For questions or issues:
- Check this documentation first
- Review code comments in auth-related files
- Test with Postman/curl to isolate frontend vs backend issues
- Check browser console and network tab for frontend issues
- Use `dotnet ef database update` if migrations fail
