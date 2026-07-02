import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import apiClient from './api/apiClient';
import { jwtDecode } from 'jwt-decode';

export interface LoginRequest {
    username: string;
    password: string;
    tenantKey: string;
}

interface LoginResponse {
    accessToken: string;
    refreshToken: string;
    userId: string;
    email: string;
    fullName: string;
    roles: string[];
    expiresAt: string;
}

interface TokenPayload {
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': string;
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': string;
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': string;
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string | string[];
    firstName?: string;
    lastName?: string;
    departmentId?: string;
    exp: number;
}

interface User {
    id: string;
    email: string;
    fullName: string;
    roles: string[];
    departmentId?: string;
}

interface AuthContextType {
    accessToken: string | null;
    refreshToken: string | null;
    user: User | null;
    login: (credentials: LoginRequest) => Promise<LoginResult>;
    logout: () => Promise<void>;
    error: string | null;
    isAuthenticated: boolean;
    hasRole: (role: string) => boolean;
}

type LoginResult =
    | { success: true }
    | { success: false; error: string };

const AuthContext = createContext<AuthContextType | null>(null);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [accessToken, setAccessToken] = useState<string | null>(localStorage.getItem('accessToken'));
    const [refreshToken, setRefreshToken] = useState<string | null>(localStorage.getItem('refreshToken'));
    const [user, setUser] = useState<User | null>(null);
    const [error, setError] = useState<string | null>(null);

    // Decode token and extract user info
    useEffect(() => {
        if (accessToken) {
            try {
                const decoded = jwtDecode<TokenPayload>(accessToken);
                const roleValue = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
                const roles = Array.isArray(roleValue) ? roleValue : [roleValue];

                setUser({
                    id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
                    email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
                    fullName: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
                    roles: roles,
                    departmentId: decoded.departmentId
                });
            } catch (err) {
                console.error('Failed to decode token:', err);
                // Token is invalid, clear it
                setAccessToken(null);
                setRefreshToken(null);
                localStorage.removeItem('accessToken');
                localStorage.removeItem('refreshToken');
                setUser(null);
            }
        } else {
            setUser(null);
        }
    }, [accessToken]);

    // Sync tokens with localStorage
    useEffect(() => {
        if (accessToken && refreshToken) {
            localStorage.setItem('accessToken', accessToken);
            localStorage.setItem('refreshToken', refreshToken);
        } else {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            setUser(null);
        }
    }, [accessToken, refreshToken]);

    const login = async (credentials: LoginRequest): Promise<LoginResult> => {
        try {
            setError(null);

            // Map username to email for the API
            const loginData = {
                email: credentials.username, // The API expects 'email'
                password: credentials.password
            };

            const response = await apiClient.post<LoginResponse>('/api/Auth/login', loginData);

            const data = response.data;

            setAccessToken(data.accessToken);
            setRefreshToken(data.refreshToken);

            return { success: true };
        } catch (error: any) {
            console.error('Authentication Error:', error);

            let errorMessage = 'Login fehlgeschlagen';

            if (error.response) {
                // Account lockout
                if (error.response.status === 423) {
                    errorMessage = error.response.data?.message || 'Konto gesperrt aufgrund mehrerer fehlgeschlagener Anmeldeversuche';
                }
                // Unauthorized
                else if (error.response.status === 401) {
                    errorMessage = 'Ungültige Anmeldedaten';
                }
                // Other errors
                else if (error.response.data?.message) {
                    errorMessage = error.response.data.message;
                }
            } else if (error.request) {
                errorMessage = 'Server nicht erreichbar';
            }

            setError(errorMessage);
            return {
                success: false,
                error: errorMessage
            };
        }
    };

    const logout = async () => {
        try {
            // Call logout endpoint to revoke refresh token
            await apiClient.post('/api/Auth/logout');
        } catch (error) {
            console.error('Logout error:', error);
        } finally {
            // Clear local state regardless of API call result
            setAccessToken(null);
            setRefreshToken(null);
            setUser(null);
            setError(null);
        }
    };

    const hasRole = (role: string): boolean => {
        return user?.roles.includes(role) ?? false;
    };

    const isAuthenticated = !!accessToken && !!user;

    return (
        <AuthContext.Provider value={{ 
            accessToken, 
            refreshToken, 
            user, 
            login, 
            logout, 
            error,
            isAuthenticated,
            hasRole
        }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
