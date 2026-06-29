import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../AuthContext';
import type { LoginRequest } from '../AuthContext';

const Login = () => {
    const navigate = useNavigate();
    const { login, accessToken, error } = useAuth();

    const [formData, setFormData] = useState<LoginRequest>({
        username: '',
        password: '',
        tenantKey: ''
    });
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [localError, setLocalError] = useState<string | null>(null);

    useEffect(() => {
        if (accessToken) {
            navigate('/dashboard');
        }
    }, [accessToken, navigate]);

    const isComplete =
        formData.username.trim().length > 0 &&
        formData.password.trim().length > 0 &&
        formData.tenantKey.trim().length > 0;

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!isComplete || isSubmitting) {
            return;
        }

        setIsSubmitting(true);
        setLocalError(null);

        const result = await login({
            username: formData.username.trim(),
            password: formData.password,
            tenantKey: formData.tenantKey.trim()
        });

        if (result.success) {
            navigate('/dashboard');
        } else {
            setLocalError(result.error);
        }

        setIsSubmitting(false);
    };

    return (
        <div style={{ maxWidth: 420, margin: '2rem auto', padding: '1rem' }}>
            <h2>Login</h2>
            <form onSubmit={handleSubmit} style={{ display: 'grid', gap: '0.75rem' }}>
                <label>
                    Username
                    <input
                        type="text"
                        value={formData.username}
                        onChange={(event) => setFormData((prev) => ({ ...prev, username: event.target.value }))}
                        autoComplete="username"
                        required
                    />
                </label>

                <label>
                    Password
                    <input
                        type="password"
                        value={formData.password}
                        onChange={(event) => setFormData((prev) => ({ ...prev, password: event.target.value }))}
                        autoComplete="current-password"
                        required
                    />
                </label>

                <label>
                    Tenant Key
                    <input
                        type="text"
                        value={formData.tenantKey}
                        onChange={(event) => setFormData((prev) => ({ ...prev, tenantKey: event.target.value }))}
                        required
                    />
                </label>

                {(localError || error) && (
                    <p style={{ color: 'red', margin: 0 }}>{localError || error}</p>
                )}

                <button type="submit" disabled={!isComplete || isSubmitting}>
                    {isSubmitting ? 'Logging in...' : 'Login'}
                </button>
            </form>
        </div>
    );
};

export default Login;
