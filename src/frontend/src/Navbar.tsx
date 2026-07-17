import React from 'react';
import { useAuth } from './AuthContext';
import { MessageIcon } from './MessageIcon';

const Navbar: React.FC = () => {
    const { isAuthenticated, logout, user } = useAuth();
    const fullName = user?.fullName || 'Benutzer';
    const roles = user?.roles || [];
    const roleDisplay = roles.length > 0 ? roles.join(', ') : 'Keine Rolle';

    return (
        <header className="titlebar">

            {isAuthenticated && user && (
                <div className="titlebar__right">
                    <MessageIcon />
                    <div className="topbar-user">
                        <div className="avatar" aria-hidden="true">
                            {getInitials(fullName)}
                        </div>
                        <div>
                            <strong>{fullName}</strong>
                            <span>{roleDisplay}</span>
                        </div>
                    </div>
                    <button className="chevron-button" type="button" onClick={logout} aria-label="Abmelden">
                        v
                    </button>
                </div>
            )} else {
                <a href="/login">Anmelden</a>
            }   
        </header>
    );
};

function getInitials(name: string) {
    return name
        .split(' ')
        .filter(Boolean)
        .map((part) => part[0])
        .join('')
        .slice(0, 2)
        .toUpperCase();
}

export default Navbar;
