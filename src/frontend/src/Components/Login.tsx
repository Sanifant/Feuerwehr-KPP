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
        password: ''
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
        formData.password.trim().length > 0;

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!isComplete || isSubmitting) {
            return;
        }

        setIsSubmitting(true);
        setLocalError(null);

        const result = await login({
            username: formData.username.trim(),
            password: formData.password
        });

        if (result.success) {
            navigate('/dashboard');
        } else {
            setLocalError(result.error);
        }

        setIsSubmitting(false);
    };

    return (
        <section className="card auth-panel" aria-labelledby="login-title">
            <h2 id="login-title">Login</h2>
            <form onSubmit={handleSubmit} className="form-stack">
                <div className="form-group">
                    <label htmlFor="username">Username</label>
                    <input
                        type="text"
                        id="username"
                        value={formData.username}
                        onChange={(event) => setFormData((prev) => ({ ...prev, username: event.target.value }))}
                        autoComplete="username"
                        required
                    />
                </div>
                <div className="form-group">
                    <label htmlFor="password">Password</label>
                    <input
                        type="password"
                        id="password"
                        value={formData.password}
                        onChange={(event) => setFormData((prev) => ({ ...prev, password: event.target.value }))}
                        autoComplete="current-password"
                        required
                    />
                </div>
                {(localError || error) && (
                    <p role="alert">{localError || error}</p>
                )}

                <button type="submit"
                    className="btn btn-primary"
                    disabled={!isComplete || isSubmitting}>
                    {isSubmitting ? 'Logging in...' : 'Login'}
                </button>
            </form>
        </section>
    );
};

export default Login;
