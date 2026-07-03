# Authentication Quick Reference Guide

## Common Tasks

### Backend

#### Create a New User (Admin Only)
```csharp
POST /api/Users
{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["Firefighter"],
    "fireDepartmentId": 1
}
```

#### Protect an Endpoint with Role
```csharp
[Authorize(Policy = PolicyNames.CanViewData)]  // All authenticated
[Authorize(Policy = PolicyNames.RequireAdmin)] // Admin only
[Authorize(Policy = PolicyNames.RequireCommander)] // Admin + Commander
[Authorize(Policy = PolicyNames.CanManageHydrants)] // Admin + Commander + Firefighter
```

#### Get Current User Info
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var email = User.FindFirstValue(ClaimTypes.Email);
var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
```

### Frontend

#### Login
```typescript
const { login } = useAuth();
await login('admin@feuerwehr.local', 'Admin@123456');
```

#### Check if Logged In
```typescript
const { isAuthenticated, user } = useAuth();

if (isAuthenticated) {
    console.log(`Welcome ${user.fullName}`);
}
```

#### Check UserRole
```typescript
const { isAdmin, canManageHydrants } = useRole();

if (isAdmin) {
    // Show admin features
}
```

#### Conditional Rendering
```tsx
<RoleGuard roles={['Admin']}>
    <AdminPanel />
</RoleGuard>

{canManageUsers && <UserManagementLink />}
```

#### Make Authenticated API Call
```typescript
import apiClient from './api/apiClient';

// Bearer token automatically added
const response = await apiClient.get('/api/Hydrant');
const data = response.data;
```

### Mobile

#### Login
```csharp
var success = await _authService.LoginAsync(email, password);
if (success)
{
    var name = _authService.UserFullName;
    var roles = _authService.UserRoles;
}
```

#### Check Authentication
```csharp
if (_authService.IsAuthenticated)
{
    // User is logged in
}
```

#### Make Authenticated Request
```csharp
var response = await _authenticatedHttpClient.GetAsync("/api/Hydrant");
if (response.IsSuccessStatusCode)
{
    var data = await response.Content.ReadFromJsonAsync<List<Hydrant>>();
}
```

#### Logout
```csharp
await _authService.LogoutAsync();
```

## Default Credentials

**Admin Account**:
- Email: `admin@feuerwehr.local`
- Password: `Admin@123456`
- ⚠️ **Change this immediately!**

## API Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/Auth/login` | POST | None | Login with email/password |
| `/api/Auth/refresh` | POST | None | Refresh access token |
| `/api/Auth/logout` | POST | Required | Revoke refresh token |
| `/api/Auth/me` | GET | Required | Get current user info |
| `/api/Users` | GET | Admin | List all users |
| `/api/Users/{id}` | GET | Admin | Get user by ID |
| `/api/Users` | POST | Admin | Create new user |
| `/api/Users/{id}` | PUT | Admin | Update user |
| `/api/Users/{id}` | DELETE | Admin | Soft delete user |

## Token Lifetimes

- **Access Token**: 30 minutes (configurable)
- **Refresh Token**: 7 days (configurable)
- **Auto-refresh**: Frontend refreshes when access token expires
- **Mobile**: Call `GetAccessTokenAsync()` before API calls

## Role Hierarchy

```
Admin (Level 4)
├─ Full system access
├─ User management
└─ All lower permissions

Commander (Level 3)
├─ Manage training courses
├─ Create fire departments
└─ View all data

Firefighter (Level 2)
├─ Manage hydrants
└─ View data

Viewer (Level 1)
└─ View data only
```

## Security Checklist

- [ ] Changed default admin password
- [ ] JWT secret moved to environment variable
- [ ] HTTPS enforced  
- [ ] Rate limiting enabled on auth endpoints
- [ ] CORS configured for production
- [ ] Secure storage implemented for mobile (platform-specific)
- [ ] Regular security audits scheduled

## Debugging Tips

**"Token expired" errors**:
```typescript
// Check token expiration in console
const token = localStorage.getItem('access_token');
console.log(JSON.parse(atob(token.split('.')[1])));
```

**Check if user has role**:
```csharp
// Backend
var hasRole = User.IsInRole("Admin");

// Frontend
const { hasRole } = useAuth();
hasRole('Admin');
```

**Clear tokens** (logout manually):
```typescript
localStorage.removeItem('access_token');
localStorage.removeItem('refresh_token');
window.location.href = '/login';
```

## Database Commands

**Apply migrations**:
```bash
cd src/Backend/Feuerwehr.Server
dotnet ef database update
```

**Reset database** (development only):
```bash
dotnet ef database drop
dotnet ef database update
# Default admin will be re-created
```

**Create new migration**:
```bash
dotnet ef migrations add MigrationName
```

## Environment Variables

**Development** (.env or appsettings.Development.json):
```json
{
  "JwtSettings": {
    "SecretKey": "development-secret-min-32-chars!!!"
  }
}
```

**Production** (Azure App Service):
```bash
az webapp config appsettings set \
    --name myapp \
    --resource-group mygroup \
    --settings JwtSettings__SecretKey="production-secret"
```

## Testing Authentication Flow

**1. Login**:
```bash
curl -X POST http://localhost:5173/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@feuerwehr.local","password":"Admin@123456"}'
```

**2. Use Access Token**:
```bash
curl -X GET http://localhost:5173/api/Users \
  -H "Authorization: Bearer eyJhbGc..."
```

**3. Refresh Token**:
```bash
curl -X POST http://localhost:5173/api/Auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"your-refresh-token-here"}'
```

## Quick Fixes

### Reset Admin Password
```sql
-- Connect to database
UPDATE AspNetUsers 
SET PasswordHash = '<new-hashed-password>'
WHERE Email = 'admin@feuerwehr.local';
```

Or programmatically:
```csharp
var user = await _userManager.FindByEmailAsync("admin@feuerwehr.local");
await _userManager.RemovePasswordAsync(user);
await _userManager.AddPasswordAsync(user, "NewSecurePass123!");
```

### Add Role to Existing User
```csharp
var user = await _userManager.FindByEmailAsync("user@example.com");
await _userManager.AddToRoleAsync(user, "Admin");
```

### Revoke All Refresh Tokens for User
```csharp
var user = await _userManager.FindByEmailAsync("user@example.com");
await _userManager.RemoveAuthenticationTokenAsync(
    user, "RefreshTokenProvider", "RefreshToken");
```
