import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';

export interface LoginRequest {
    username: string;
    password: string;
    tenantKey: string;
}

interface LoginResponse {
    accessToken: string;
    refreshToken: string;
}

interface User {
    id: string;
    username: string;
}

interface AuthContextType {
    accessToken: string | null;
    refreshToken: string | null;
    user: User | null;
    login: (credentials: LoginRequest) => Promise<LoginResult>;
    logout: () => void;
    error: string | null;
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

    const getApiUrl = () => {
        const serverUrl = import.meta.env.VITE_API_URL;
        return serverUrl || 'https://auth.grinch-tech.de/';
    };

    const login = async (credentials: LoginRequest): Promise<LoginResult> => {
        try {
            setError(null);
            const baseUrl = getApiUrl();
            const targetUrl = `${baseUrl.replace(/\/$/, '')}/api/auth/login`;

            console.log('Logging in to:', targetUrl);

            /*
            const response = await fetch(targetUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(credentials),
            });
            */
            
            const response = {
                ok: true,
                status: 200,
                json: async (): Promise<LoginResponse> => ({
                    accessToken: '123', 
                    refreshToken: '321'
                })
            };
            
            console.timeLog("Testing")
            // Account Lockout Handling
            if (response.status === 423) {
                const errorText = "Fehler"; //await response.text();
                setError(errorText);
                return {
                    success: false,
                    error: errorText
                };
            }

            if (!response.ok) {
                const errorText = "Fehler"; //await response.text();
                setError(`Login fehlgeschlagen: ${errorText}`);
                return {
                    success: false,
                    error: errorText
                };
            }

            const data = await response.json();

            // Successful login without 2FA
            const loginData = data as LoginResponse;
            setAccessToken(loginData.accessToken);
            setRefreshToken(loginData.refreshToken);
            setUser({ id: '1', username: credentials.username });

            return { success: true };
        } catch (error) {
            console.error('Authentication Error:', error);
            const errorMessage = error instanceof Error ? error.message : 'Unbekannter Fehler';
            setError(errorMessage);
            return {
                success: false,
                error: errorMessage
            };
        }
    };

    const logout = () => {
        setAccessToken(null);
        setRefreshToken(null);
        setError(null);
    };

    return (
        <AuthContext.Provider value={{ accessToken, refreshToken, user, login, logout, error }}>
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