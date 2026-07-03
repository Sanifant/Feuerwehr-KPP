import React, { useEffect, useState } from 'react';
import apiClient from '../api/apiClient';
import { useRole } from '../hooks/useRole';
import { Navigate } from 'react-router-dom';

const AVAILABLE_ROLES = ['Admin', 'Commander', 'Firefighter', 'Viewer'];

interface User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
    isActive: boolean;
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

    const [showCreateModal, setShowCreateModal] = useState(false);
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
        } catch (err: any) {
            setError(err.response?.data?.message || 'Fehler beim Laden der Benutzer');
            console.error('Error fetching users:', err);
        } finally {
            setLoading(false);
        }
    };

    const openCreateModal = () => {
        setForm(EMPTY_FORM);
        setFormErrors({});
        setSubmitError(null);
        setShowCreateModal(true);
    };

    const closeCreateModal = () => {
        setShowCreateModal(false);
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
        if (!form.password) {
            errors.password = 'Passwort ist erforderlich.';
        } else if (form.password.length < 6) {
            errors.password = 'Passwort muss mindestens 6 Zeichen lang sein.';
        }
        if (form.password !== form.confirmPassword) {
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
            await apiClient.post('/api/Users', {
                email: form.email,
                password: form.password,
                firstName: form.firstName,
                lastName: form.lastName,
                roles: form.roles,
            });
            closeCreateModal();
            await fetchUsers();
        } catch (err: any) {
            const serverMessage = err.response?.data?.message;
            const identityErrors = err.response?.data?.errors;
            if (identityErrors && Array.isArray(identityErrors)) {
                setSubmitError(identityErrors.map((e: any) => e.description).join(' '));
            } else {
                setSubmitError(serverMessage || 'Fehler beim Erstellen des Benutzers.');
            }
        } finally {
            setSubmitting(false);
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
                                    <span className={`badge ${user.isActive ? 'badge-success' : 'badge-inactive'}`}>
                                        {user.isActive ? 'Aktiv' : 'Inaktiv'}
                                    </span>
                                </td>
                                <td>{new Date(user.createdAt).toLocaleDateString('de-DE')}</td>
                                <td>
                                    {user.lastLoginAt
                                        ? new Date(user.lastLoginAt).toLocaleString('de-DE')
                                        : 'Nie'}
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
                            <h2>Neuen Benutzer erstellen</h2>
                            <button className="modal-close" onClick={closeCreateModal} aria-label="Schließen">
                                ×
                            </button>
                        </div>

                        {submitError && (
                            <div className="alert alert-error">{submitError}</div>
                        )}

                        <form onSubmit={handleSubmit} noValidate>
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

                            <div className="form-row">
                                <div className="form-group">
                                    <label htmlFor="password">Passwort *</label>
                                    <input
                                        id="password"
                                        name="password"
                                        type="password"
                                        value={form.password}
                                        onChange={handleFieldChange}
                                        className={formErrors.password ? 'input-error' : ''}
                                        disabled={submitting}
                                        autoComplete="new-password"
                                    />
                                    {formErrors.password && <span className="field-error">{formErrors.password}</span>}
                                </div>
                                <div className="form-group">
                                    <label htmlFor="confirmPassword">Passwort bestätigen *</label>
                                    <input
                                        id="confirmPassword"
                                        name="confirmPassword"
                                        type="password"
                                        value={form.confirmPassword}
                                        onChange={handleFieldChange}
                                        className={formErrors.confirmPassword ? 'input-error' : ''}
                                        disabled={submitting}
                                        autoComplete="new-password"
                                    />
                                    {formErrors.confirmPassword && <span className="field-error">{formErrors.confirmPassword}</span>}
                                </div>
                            </div>

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
                                    {submitting ? 'Wird erstellt…' : 'Benutzer erstellen'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            <style>{`
                .user-management {
                    padding: 2rem;
                }

                .page-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: flex-start;
                    margin-bottom: 1.5rem;
                }

                .page-header h1 {
                    margin: 0 0 0.25rem 0;
                }

                .text-muted {
                    color: #6c757d;
                    margin: 0;
                }

                .users-table {
                    overflow-x: auto;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    background: white;
                    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
                }

                th, td {
                    padding: 0.75rem;
                    text-align: left;
                    border-bottom: 1px solid #dee2e6;
                }

                th {
                    background-color: #f8f9fa;
                    font-weight: 600;
                }

                .badge {
                    display: inline-block;
                    padding: 0.25rem 0.5rem;
                    font-size: 0.75rem;
                    font-weight: 600;
                    border-radius: 0.25rem;
                    margin-right: 0.25rem;
                }

                .badge-role { background-color: #0d6efd; color: white; }
                .badge-success { background-color: #198754; color: white; }
                .badge-inactive { background-color: #6c757d; color: white; }

                .alert {
                    padding: 1rem;
                    border-radius: 0.25rem;
                    margin-bottom: 1rem;
                }

                .alert-error {
                    background-color: #f8d7da;
                    color: #842029;
                    border: 1px solid #f5c2c7;
                }

                /* Buttons */
                .btn {
                    padding: 0.5rem 1.1rem;
                    border: none;
                    border-radius: 0.375rem;
                    font-size: 0.9rem;
                    font-weight: 600;
                    cursor: pointer;
                    transition: background-color 0.15s;
                }

                .btn:disabled { opacity: 0.65; cursor: not-allowed; }
                .btn-primary { background-color: #0d6efd; color: white; }
                .btn-primary:hover:not(:disabled) { background-color: #0b5ed7; }
                .btn-secondary { background-color: #6c757d; color: white; }
                .btn-secondary:hover:not(:disabled) { background-color: #5c636a; }

                /* Modal */
                .modal-backdrop {
                    position: fixed;
                    inset: 0;
                    background: rgba(0, 0, 0, 0.5);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 1000;
                }

                .modal {
                    background: white;
                    border-radius: 0.5rem;
                    width: 100%;
                    max-width: 560px;
                    max-height: 90vh;
                    overflow-y: auto;
                    padding: 1.75rem;
                    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
                }

                .modal-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 1.5rem;
                }

                .modal-header h2 {
                    margin: 0;
                    font-size: 1.25rem;
                }

                .modal-close {
                    background: none;
                    border: none;
                    font-size: 1.75rem;
                    line-height: 1;
                    cursor: pointer;
                    color: #6c757d;
                    padding: 0;
                }

                .modal-close:hover { color: #000; }

                .modal-footer {
                    display: flex;
                    justify-content: flex-end;
                    gap: 0.75rem;
                    margin-top: 1.5rem;
                    padding-top: 1rem;
                    border-top: 1px solid #dee2e6;
                }

                /* Form */
                .form-row {
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 1rem;
                }

                .form-group {
                    display: flex;
                    flex-direction: column;
                    margin-bottom: 1rem;
                }

                .form-group label {
                    font-weight: 600;
                    font-size: 0.875rem;
                    margin-bottom: 0.35rem;
                    color: #333;
                }

                .form-group input[type="text"],
                .form-group input[type="email"],
                .form-group input[type="password"] {
                    padding: 0.5rem 0.75rem;
                    border: 1px solid #ced4da;
                    border-radius: 0.375rem;
                    font-size: 0.9rem;
                    transition: border-color 0.15s;
                }

                .form-group input:focus {
                    outline: none;
                    border-color: #0d6efd;
                    box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.15);
                }

                .input-error {
                    border-color: #dc3545 !important;
                }

                .input-error-border {
                    border-color: #dc3545;
                }

                .field-error {
                    color: #dc3545;
                    font-size: 0.8rem;
                    margin-top: 0.25rem;
                }

                /* Role Checkboxes */
                .role-checkboxes {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 0.5rem;
                    padding: 0.5rem;
                    border: 1px solid #ced4da;
                    border-radius: 0.375rem;
                }

                .role-checkbox-label {
                    display: flex;
                    align-items: center;
                    gap: 0.375rem;
                    font-weight: normal !important;
                    font-size: 0.9rem;
                    cursor: pointer;
                    padding: 0.25rem 0.5rem;
                    border-radius: 0.25rem;
                    transition: background-color 0.1s;
                }

                .role-checkbox-label:hover {
                    background-color: #f0f4ff;
                }

                .role-checkbox-label input[type="checkbox"] {
                    cursor: pointer;
                    width: 1rem;
                    height: 1rem;
                    accent-color: #0d6efd;
                }
            `}</style>
        </div>
    );
};

export default UserManagement;
