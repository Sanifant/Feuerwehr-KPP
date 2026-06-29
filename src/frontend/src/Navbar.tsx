import React from 'react';
import { useAuth } from './AuthContext';
import { MessageIcon } from './MessageIcon';

const Navbar: React.FC = () => {
    const { accessToken, logout, user } = useAuth();
    const username = user?.username || 'Max Mustermann';

    return (
        <header className="titlebar">

            {accessToken && (
                <div className="titlebar__right">
                    <MessageIcon />
                    <div className="topbar-user">
                        <div className="avatar" aria-hidden="true">
                            {getInitials(username)}
                        </div>
                        <div>
                            <strong>{username}</strong>
                            <span>Administrator</span>
                        </div>
                    </div>
                    <button className="chevron-button" type="button" onClick={logout} aria-label="Abmelden">
                        v
                    </button>
                </div>
    )}
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
