import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from './AuthContext';

const Navbar: React.FC = () => {
    const { accessToken, logout, user } = useAuth();

    return (
        <nav style={styles.navbar}>
            <div style={styles.logo}>Sanifant</div>

            <ul style={styles.navLinks}>
                {/* WENN ANGEMELDET: Zeige alle internen Links */}
                {accessToken ? (
                    <>
                        <li>
                            <NavLink to="/dashboard" style={({ isActive }) => isActive ? styles.activeLink : styles.link}>
                                Dashboard
                            </NavLink>
                        </li>
                        <li>
                            <NavLink to="/tenant" style={({ isActive }) => isActive ? styles.activeLink : styles.link}>
                                Mandantenverwaltung
                            </NavLink>
                        </li>
                        <li>
                            <NavLink to="/users" style={({ isActive }) => isActive ? styles.activeLink : styles.link}>
                                Benutzerverwaltung
                            </NavLink>
                        </li>

                        {/* Benutzerinfo & Logout-Button */}
                        <li style={styles.userInfo}>
                            <span>Hallo, {user?.username || 'User'}</span>
                            <button onClick={logout} style={styles.logoutButton}>Abmelden</button>
                        </li>
                    </>
                ) : (
                        /* WENN ABGEMELDET: Zeige nur den Login-Link */
                    <>
                        <li>
                            <NavLink to="/hydrant" style={({ isActive }) => isActive ? styles.activeLink : styles.link}>
                                Hydranten
                            </NavLink>
                        </li>
                        <li>
                            <NavLink to="/trainingdashboard" style={({ isActive }) => isActive ? styles.activeLink : styles.link}>
                                Lehrgänge
                            </NavLink>
                        </li>
                        <li>
                            <NavLink to="/login" style={({ isActive }) => isActive ? styles.activeLink : styles.link}>
                                Login
                            </NavLink>
                        </li>
                    </>
                )}
            </ul>
        </nav>
    );
};

// Einfache Inline-Styles zur Veranschaulichung
const styles = {
    navbar: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '1rem 2rem',
        backgroundColor: '#2c3e50',
        color: '#fff',
    },
    logo: {
        fontSize: '1.5rem',
        fontWeight: 'bold',
    },
    navLinks: {
        display: 'flex',
        listStyle: 'none',
        gap: '20px',
        alignItems: 'center',
        margin: 0,
        padding: 0,
    },
    link: {
        color: '#bdc3c7',
        textDecoration: 'none',
    },
    activeLink: {
        color: '#fff',
        fontWeight: 'bold',
        textDecoration: 'underline',
    },
    userInfo: {
        display: 'flex',
        gap: '15px',
        alignItems: 'center',
        marginLeft: '20px',
        borderLeft: '1px solid #7f8c8d',
        paddingLeft: '20px',
    },
    logoutButton: {
        backgroundColor: '#e74c3c',
        color: 'white',
        border: 'none',
        padding: '5px 10px',
        cursor: 'pointer',
        borderRadius: '4px',
    }
};

export default Navbar;