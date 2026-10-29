import React, { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { MessageIcon } from './MessageIcon';
import IncidentTitlebar from './Components/IncidentTitlebar';

const Navbar: React.FC = () => {
    const { isAuthenticated, logout, user } = useAuth();
    const fullName = user?.fullName || 'Benutzer';
    const email = user?.email || 'Keine E-Mail';
    const roles = user?.roles || [];
    const roleDisplay = roles.length > 0 ? roles.join(', ') : 'Keine Rolle';
    const [menuOpen, setMenuOpen] = useState(false);
    const menuId = useId();
    const menuContainer = useRef<HTMLDivElement>(null);
    const menuButton = useRef<HTMLButtonElement>(null);
    const passwordLink = useRef<HTMLAnchorElement>(null);

    useEffect(() => {
        if (!menuOpen) return;
        passwordLink.current?.focus();
        const closeOutside = (event: PointerEvent) => {
            if (!menuContainer.current?.contains(event.target as Node)) setMenuOpen(false);
        };
        document.addEventListener('pointerdown', closeOutside);
        return () => document.removeEventListener('pointerdown', closeOutside);
    }, [menuOpen]);

    function handleMenuKeys(event: React.KeyboardEvent<HTMLDivElement>) {
        if (event.key === 'Escape') {
            event.preventDefault();
            setMenuOpen(false);
            menuButton.current?.focus();
        }
        if (menuOpen && ['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
            event.preventDefault();
            const items = [...menuContainer.current!.querySelectorAll<HTMLElement>('[role="menuitem"]')];
            const current = items.indexOf(document.activeElement as HTMLElement);
            const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1
                : (current + (event.key === 'ArrowUp' ? -1 : 1) + items.length) % items.length;
            items[next]?.focus();
        }
    }

    return (
        <header className="titlebar">
            {isAuthenticated && user && <IncidentTitlebar />}

            {isAuthenticated && user && (
                <div className="titlebar__right">
                    <MessageIcon />
                    <div className="topbar-user">
                        <div className="avatar" aria-hidden="true">
                            {getInitials(fullName)}
                        </div>
                        <div>
                            <strong>{fullName}</strong>
                            <span>{email}</span>
                        </div>
                    </div>
                    <div className="user-menu" ref={menuContainer} onKeyDown={handleMenuKeys}
                        onBlur={event => {
                            if (!event.currentTarget.contains(event.relatedTarget)) setMenuOpen(false);
                        }}>
                        <button className="chevron-button" type="button" ref={menuButton}
                            onClick={() => setMenuOpen(open => !open)} aria-label="Benutzermenü"
                            aria-haspopup="menu" aria-expanded={menuOpen} aria-controls={menuId}
                            onKeyDown={event => {
                                if (!menuOpen && ['ArrowDown', 'ArrowUp'].includes(event.key)) {
                                    event.preventDefault();
                                    setMenuOpen(true);
                                }
                            }}>
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                strokeWidth="2" aria-hidden="true"><path d="m6 9 6 6 6-6" /></svg>
                        </button>
                        {menuOpen && <div className="user-menu__items" id={menuId} role="menu" aria-label="Benutzerkonto">
                            
                            <div>
                                <span>{roleDisplay}</span>
                            </div>
                            <hr />
                            <a href="https://openelp.gitbook.io/feuerwehr-kpp-documentation" role="menuitem">
                                Dokumentation
                            </a>
                            <hr />
                            <Link to="/password" role="menuitem" ref={passwordLink} onClick={() => setMenuOpen(false)}>
                                Passwort ändern
                            </Link>
                            <button type="button" role="menuitem" onClick={() => { setMenuOpen(false); void logout(); }}>
                                Abmelden
                            </button>
                        </div>}
                    </div>
                </div>
            )}
            {!isAuthenticated && (
                <div className="titlebar__right">
                    <a className="chevron-button" href="/login">Anmelden</a>
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
