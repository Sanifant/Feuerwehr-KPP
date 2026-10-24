import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { changeOwnPassword, setInvitedUserPassword } from '../api/passwordApi';
import { getApiErrorMessage } from '../api/errors';
import { useAuth } from '../AuthContext';

export function ChangePassword() {
    const { token } = useParams<{ token: string }>();
    return token === undefined ? <AccountPasswordChange /> : <PasswordForm token={token} />;
}

function AccountPasswordChange() {
    const { user, logout } = useAuth();
    if (!user) return <p>Benutzer wird geladen…</p>;
    return <PasswordForm accountEmail={user.email} onSignIn={logout} />;
}

function PasswordForm({ token, accountEmail, onSignIn }: {
    token?: string; accountEmail?: string; onSignIn?: () => Promise<void>;
}) {
    const isAccountChange = token === undefined;
    const [email, setEmail] = useState('');
    const [currentPassword, setCurrentPassword] = useState('');
    const [password, setPassword] = useState('');
    const [repeatPassword, setRepeatPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const [complete, setComplete] = useState(false);
    const validToken = isAccountChange || /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(token ?? '')
        && token !== '00000000-0000-0000-0000-000000000000';
    const effectiveEmail = isAccountChange ? accountEmail ?? '' : email;

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (submitting || !validToken) return;
        setError(null);
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(effectiveEmail.trim())) {
            setError('Bitte geben Sie Ihre E-Mail-Adresse ein.');
            return;
        }
        if (isAccountChange && !currentPassword) {
            setError('Bitte geben Sie Ihr aktuelles Passwort ein.');
            return;
        }
        if (password !== repeatPassword) {
            setError('Die Passwörter stimmen nicht überein.');
            return;
        }
        if (password.length < 8 || !/[a-z]/.test(password) || !/[A-Z]/.test(password) || !/\d/.test(password)) {
            setError('Das Passwort muss mindestens 8 Zeichen sowie Groß- und Kleinbuchstaben und eine Zahl enthalten.');
            return;
        }
        setSubmitting(true);
        try {
            if (isAccountChange) await changeOwnPassword({ currentPassword, newPassword: password });
            else await setInvitedUserPassword({ token: token!, email: effectiveEmail.trim(), newPassword: password });
            setCurrentPassword('');
            setPassword('');
            setRepeatPassword('');
            setComplete(true);
        } catch (cause: unknown) {
            setError(getApiErrorMessage(cause, 'Das Passwort konnte nicht gespeichert werden. Bitte versuchen Sie es erneut.'));
        } finally {
            setSubmitting(false);
        }
    }

    if (!validToken) return <p role="alert">Der Einladungslink ist ungültig.</p>;
    if (complete) return (
        <div className="card auth-panel">
            <p role="status" className="alert alert-success">Ihr Passwort wurde gespeichert. Sie können sich jetzt anmelden.</p>
            {isAccountChange
                ? <button type="button" onClick={() => { void onSignIn?.(); }}>Erneut anmelden</button>
                : <Link to="/login">Zur Anmeldung</Link>}
        </div>
    );

    return (
        <section className="card auth-panel change-password">
            <h1>{isAccountChange ? 'Passwort ändern' : 'Passwort festlegen'}</h1>
            <p>{isAccountChange ? 'Bestätigen Sie Ihr aktuelles Passwort und wählen Sie ein neues Passwort.'
                : 'Geben Sie die E-Mail-Adresse Ihrer Einladung ein und wählen Sie Ihr Passwort.'}</p>
            {error && <p role="alert">{error}</p>}
            <form onSubmit={handleSubmit} className="form-stack" noValidate>
                <div className="form-group">
                    <label htmlFor="invitation-email">E-Mail-Adresse</label>
                    <input id="invitation-email" type="email" autoComplete="username" required
                        value={effectiveEmail} readOnly={isAccountChange}
                        onChange={event => setEmail(event.target.value)} disabled={submitting} />
                </div>
                {isAccountChange && <div className="form-group">
                    <label htmlFor="current-password">Aktuelles Passwort</label>
                    <input id="current-password" type="password" autoComplete="current-password" required
                        value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} disabled={submitting} />
                </div>}
                <div className="form-group">
                    <label htmlFor="new-password">Neues Passwort</label>
                    <input id="new-password" type="password" autoComplete="new-password" required minLength={8}
                        value={password} onChange={event => setPassword(event.target.value)} disabled={submitting} />
                </div>
                <div className="form-group">
                    <label htmlFor="repeat-password">Passwort wiederholen</label>
                    <input id="repeat-password" type="password" autoComplete="new-password" required
                        value={repeatPassword} onChange={event => setRepeatPassword(event.target.value)} disabled={submitting} />
                </div>
                <button type="submit" disabled={submitting}>{submitting ? 'Wird gespeichert…' : 'Passwort speichern'}</button>
            </form>
        </section>
    );
}
