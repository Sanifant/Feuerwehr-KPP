import './UserManagement.css';
import React, { useEffect, useState } from 'react';
import apiClient from '../api/apiClient';
import { getApiErrorMessage } from '../api/errors';
import { useRole } from '../hooks/useRole';
import { Navigate } from 'react-router-dom';

const AVAILABLE_ROLES = ['Admin', 'Commander', 'Firefighter', 'Viewer', 'SituationMapViewer','SituationMapEditor', 'IncidentDiaryViewer', 'IncidentDiaryEditor'];

interface User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
    isActive: boolean;
    passwordResetRequired: boolean;
    roles: string[];
    createdAt: string;
    lastLoginAt?: string;
}

interface CreateUserForm {
    email: string;
    password: string;
    confirmPassword: string;
    firstName: string;
    lastName: string;
    roles: string[];
}

interface CreateUserFormErrors {
    email?: string;
    password?: string;
    confirmPassword?: string;
    firstName?: string;
    lastName?: string;
    roles?: string;
}

const EMPTY_FORM: CreateUserForm = {
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    roles: [],
};

const UserManagement: React.FC = () => {
    const { isAdmin } = useRole();
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [notice, setNotice] = useState<string | null>(null);
    const [invitationError, setInvitationError] = useState<string | null>(null);
    const [sendingInvitations, setSendingInvitations] = useState<string[]>([]);

    const [showCreateModal, setShowCreateModal] = useState(false);
    const [editingUser, setEditingUser] = useState<User | null>(null);
    const [form, setForm] = useState<CreateUserForm>(EMPTY_FORM);
    const [formErrors, setFormErrors] = useState<CreateUserFormErrors>({});
    const [submitting, setSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);

    useEffect(() => {
        if (isAdmin) {
            fetchUsers();
        }
    }, [isAdmin]);

    const fetchUsers = async () => {
        try {
            setLoading(true);
            const response = await apiClient.get<User[]>('/api/Users');
            setUsers(response.data);
            setError(null);
        } catch (err: unknown) {
            setError(getApiErrorMessage(err, 'Fehler beim Laden der Benutzer'));
            console.error('Error fetching users:', err);
        } finally {
            setLoading(false);
        }
    };

    const openCreateModal = () => {
        setEditingUser(null);
        setForm(EMPTY_FORM);
        setFormErrors({});
        setSubmitError(null);
        setShowCreateModal(true);
    };

    const openEditModal = (user: User) => {
        setEditingUser(user);
        setForm({
            email: user.email,
            password: '',
            confirmPassword: '',
            firstName: user.firstName,
            lastName: user.lastName,
            roles: [...user.roles],
        });
        setFormErrors({});
        setSubmitError(null);
        setShowCreateModal(true);
    };

    const closeCreateModal = () => {
        setShowCreateModal(false);
        setEditingUser(null);
    };

    const handleFieldChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm(prev => ({ ...prev, [name]: value }));
        setFormErrors(prev => ({ ...prev, [name]: undefined }));
    };

    const handleRoleToggle = (role: string) => {
        setForm(prev => ({
            ...prev,
            roles: prev.roles.includes(role)
                ? prev.roles.filter(r => r !== role)
                : [...prev.roles, role],
        }));
        setFormErrors(prev => ({ ...prev, roles: undefined }));
    };

    const validate = (): boolean => {
        const errors: CreateUserFormErrors = {};

        if (!form.firstName.trim()) errors.firstName = 'Vorname ist erforderlich.';
        if (!form.lastName.trim()) errors.lastName = 'Nachname ist erforderlich.';
        if (!form.email.trim()) {
            errors.email = 'E-Mail ist erforderlich.';
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
            errors.email = 'Ungültige E-Mail-Adresse.';
        }
        if (editingUser && form.password && (form.password.length < 8 || !/[a-z]/.test(form.password) || !/[A-Z]/.test(form.password) || !/\d/.test(form.password))) {
            errors.password = 'Passwort muss mindestens 8 Zeichen sowie Groß- und Kleinbuchstaben und eine Zahl enthalten.';
        }
        if (editingUser && form.password !== form.confirmPassword) {
            errors.confirmPassword = 'Passwörter stimmen nicht überein.';
        }
        if (form.roles.length === 0) {
            errors.roles = 'Mindestens eine Rolle muss ausgewählt werden.';
        }

        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validate()) return;

        setSubmitting(true);
        setSubmitError(null);

        try {
            const userRequest = {
                email: form.email.trim(),
                firstName: form.firstName,
                lastName: form.lastName,
                roles: form.roles,
            };
            if (editingUser) {
                await apiClient.put(`/api/Users/${editingUser.id}`, {
                    ...userRequest,
                    newPassword: form.password || undefined,
                });
            } else {
                await apiClient.post('/api/Users', userRequest);
                setNotice('Benutzer erstellt. Die Einladung zur Passwortvergabe wurde per E-Mail versendet.');
            }
            closeCreateModal();
            await fetchUsers();
        } catch (err: unknown) {
            setSubmitError(getApiErrorMessage(err, `Fehler beim ${editingUser ? 'Bearbeiten' : 'Erstellen'} des Benutzers.`));
        } finally {
            setSubmitting(false);
        }
    };

    const resendInvitation = async (user: User) => {
        setSendingInvitations(prev => [...prev, user.id]);
        setNotice(null);
        setInvitationError(null);
        try {
            await apiClient.post(`/api/Users/${user.id}/resend-invitation`);
            setUsers(prev => prev.map(current => current.id === user.id
                ? { ...current, passwordResetRequired: true }
                : current));
            setNotice(`Einladung an ${user.email} erneut versendet. Das Konto ist bis zur Passwortvergabe gesperrt.`);
        } catch (err: unknown) {
            setInvitationError(getApiErrorMessage(err, 'Die Einladung konnte nicht erneut versendet werden. Bitte versuchen Sie es erneut.'));
        } finally {
            setSendingInvitations(prev => prev.filter(id => id !== user.id));
        }
    };

    // Redirect non-admins
    if (!isAdmin) {
        return <Navigate to="/dashboard" replace />;
    }

    if (loading) {
        return <div className="user-management">Lade Benutzer...</div>;
    }

    if (error) {
        return (
            <div className="user-management">
                <div className="alert alert-error">{error}</div>
            </div>
        );
    }

    return (
        <div className="user-management">
            <div className="page-header">
                <div>
                    <h1>Benutzerverwaltung</h1>
                    <p className="text-muted">Nur für Administratoren</p>
                </div>
                <button className="btn btn-primary" onClick={openCreateModal}>
                    + Neuen Benutzer erstellen
                </button>
            </div>

            {notice && <p role="status">{notice}</p>}
            {invitationError && <p className="alert alert-error" role="alert">{invitationError}</p>}
            <div className="users-table">
                <table>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>E-Mail</th>
                            <th>Rollen</th>
                            <th>Status</th>
                            <th>Erstellt am</th>
                            <th>Letzter Login</th>
                            <th>Aktionen</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(user => (
                            <tr key={user.id}>
                                <td>{user.fullName}</td>
                                <td>{user.email}</td>
                                <td>
                                    {user.roles.map(role => (
                                        <span key={role} className="badge badge-role">
                                            {role}
                                        </span>
                                    ))}
                                </td>
                                <td>
                                    <span className={`badge ${user.isActive && !user.passwordResetRequired ? 'badge-success' : 'badge-inactive'}`}>
                                        {!user.isActive ? 'Inaktiv' : user.passwordResetRequired ? 'Gesperrt – Passwortvergabe ausstehend' : 'Aktiv'}
                                    </span>
                                </td>
                                <td>{new Date(user.createdAt).toLocaleDateString('de-DE')}</td>
                                <td>
                                    {user.lastLoginAt
                                        ? new Date(user.lastLoginAt).toLocaleString('de-DE')
                                        : 'Nie'}
                                </td>
                                <td>
                                    <button
                                        type="button"
                                        className="user-edit-button"
                                        onClick={() => openEditModal(user)}
                                        aria-label={`${user.fullName} bearbeiten`}
                                        title="Benutzer bearbeiten"
                                    >
                                        <svg viewBox="0 0 24 24" aria-hidden="true">
                                            <path d="M12 20h9" />
                                            <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L8 18l-4 1 1-4Z" />
                                        </svg>
                                    </button>
                                    <button
                                        type="button"
                                        className="btn btn-secondary user-invitation-button"
                                        onClick={() => resendInvitation(user)}
                                        disabled={!user.isActive || sendingInvitations.includes(user.id)}
                                        aria-label={`Einladung für ${user.fullName} erneut senden`}
                                        title="Sperrt das Konto bis zur Passwortvergabe über den neuen Einladungslink"
                                    >
                                        {sendingInvitations.includes(user.id) ? 'Wird versendet…' : 'Einladung erneut senden'}
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* Create User Modal */}
            {showCreateModal && (
                <div className="modal-backdrop" onClick={closeCreateModal}>
                    <div className="modal" onClick={e => e.stopPropagation()}>
                        <div className="modal-header">
                            <h2>{editingUser ? 'Benutzer bearbeiten' : 'Neuen Benutzer erstellen'}</h2>
                            <button className="modal-close" onClick={closeCreateModal} aria-label="Schließen">
                                ×
                            </button>
                        </div>

                        {submitError && (
                            <div className="alert alert-error">{submitError}</div>
                        )}

                        <form onSubmit={handleSubmit} className="form-stack" noValidate>
                            <div className="form-row">
                                <div className="form-group">
                                    <label htmlFor="firstName">Vorname *</label>
                                    <input
                                        id="firstName"
                                        name="firstName"
                                        type="text"
                                        value={form.firstName}
                                        onChange={handleFieldChange}
                                        className={formErrors.firstName ? 'input-error' : ''}
                                        disabled={submitting}
                                        autoComplete="given-name"
                                    />
                                    {formErrors.firstName && <span className="field-error">{formErrors.firstName}</span>}
                                </div>
                                <div className="form-group">
                                    <label htmlFor="lastName">Nachname *</label>
                                    <input
                                        id="lastName"
                                        name="lastName"
                                        type="text"
                                        value={form.lastName}
                                        onChange={handleFieldChange}
                                        className={formErrors.lastName ? 'input-error' : ''}
                                        disabled={submitting}
                                        autoComplete="family-name"
                                    />
                                    {formErrors.lastName && <span className="field-error">{formErrors.lastName}</span>}
                                </div>
                            </div>

                            <div className="form-group">
                                <label htmlFor="email">E-Mail-Adresse *</label>
                                <input
                                    id="email"
                                    name="email"
                                    type="email"
                                    value={form.email}
                                    onChange={handleFieldChange}
                                    className={formErrors.email ? 'input-error' : ''}
                                    disabled={submitting}
                                    autoComplete="email"
                                />
                                {formErrors.email && <span className="field-error">{formErrors.email}</span>}
                            </div>

                            {!editingUser && <p>Der Benutzer erhält eine Einladung per E-Mail und legt sein Passwort selbst fest.</p>}
                            {editingUser && (
                            <div className="form-row">
                                <div className="form-group">
                                    <label htmlFor="password">Passwort {editingUser ? '(optional)' : '*'}</label>
                                    <input
                                        id="password"
                                        name="password"
                                        type="password"
                                        value={form.password}
                                        onChange={handleFieldChange}
                                        className={formErrors.password ? 'input-error' : ''}
                                        disabled={submitting || editingUser.passwordResetRequired}
                                        autoComplete="new-password"
                                    />
                                    {formErrors.password && <span className="field-error">{formErrors.password}</span>}
                                </div>
                                <div className="form-group">
                                    <label htmlFor="confirmPassword">Passwort bestätigen {editingUser ? '(optional)' : '*'}</label>
                                    <input
                                        id="confirmPassword"
                                        name="confirmPassword"
                                        type="password"
                                        value={form.confirmPassword}
                                        onChange={handleFieldChange}
                                        className={formErrors.confirmPassword ? 'input-error' : ''}
                                        disabled={submitting || editingUser.passwordResetRequired}
                                        autoComplete="new-password"
                                    />
                                    {formErrors.confirmPassword && <span className="field-error">{formErrors.confirmPassword}</span>}
                                </div>
                            </div>

                            )}
                            {editingUser?.passwordResetRequired && <p>Das Konto ist gesperrt, bis der Benutzer sein Passwort über den Einladungslink festgelegt hat.</p>}

                            <div className="form-group">
                                <label>Rollen *</label>
                                <div className={`role-checkboxes ${formErrors.roles ? 'input-error-border' : ''}`}>
                                    {AVAILABLE_ROLES.map(role => (
                                        <label key={role} className="role-checkbox-label">
                                            <input
                                                type="checkbox"
                                                checked={form.roles.includes(role)}
                                                onChange={() => handleRoleToggle(role)}
                                                disabled={submitting}
                                            />
                                            {role}
                                        </label>
                                    ))}
                                </div>
                                {formErrors.roles && <span className="field-error">{formErrors.roles}</span>}
                            </div>

                            <div className="modal-footer">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={closeCreateModal}
                                    disabled={submitting}
                                >
                                    Abbrechen
                                </button>
                                <button
                                    type="submit"
                                    className="btn btn-primary"
                                    disabled={submitting}
                                >
                                    {submitting
                                        ? (editingUser ? 'Wird gespeichert…' : 'Wird erstellt…')
                                        : (editingUser ? 'Änderungen speichern' : 'Benutzer erstellen')}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

        </div>
    );
};

export default UserManagement;
