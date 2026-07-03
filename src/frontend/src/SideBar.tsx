import { useEffect, useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from './AuthContext';
import { useRole } from './hooks/useRole';

type SidebarGroup = 'lehrgaenge' | 'inventory' | 'verwaltung';

export function SideBar() {
    const { isAuthenticated } = useAuth();
    const { canManageUsers, isFirefighter, canManageTraining } = useRole();
    const location = useLocation();
    const navigate = useNavigate();
    const currentPath = `${location.pathname}${location.search}`;
    const [activeGroup, setActiveGroup] = useState<SidebarGroup | null>(() => getActiveGroup(currentPath));

    useEffect(() => {
        const group = getActiveGroup(currentPath);

        if (group) {
            setActiveGroup(group);
        }
    }, [currentPath]);

    return(
        <aside className="sidebar">
            <div className="brand">
                <div className="brand__mark" aria-hidden="true">F</div>
                <div>Feuerwehr</div>
            </div>

            <nav>
                {isAuthenticated ? (
                    <>
                        <SidebarLink to="/dashboard" currentPath={currentPath}>
                            <span className="nav-icon" aria-hidden="true">⌂</span>
                            Dashboard
                        </SidebarLink>

                        {isFirefighter && (
                            <SidebarLink to="/hydrant" currentPath={currentPath}>
                                <span className="nav-icon" aria-hidden="true"></span>
                                Hydrant
                            </SidebarLink>
                        )}

                        {canManageTraining && (
                            <>
                            <SidebarGroupButton
                            isActive={activeGroup === 'lehrgaenge'}
                            to="/trainingdashboard"
                            onClick={(to) => {
                                setActiveGroup(activeGroup === 'lehrgaenge' ? null : 'lehrgaenge');

                                if (to) {
                                    navigate(to);
                                }
                            }}
                        >
                            <span className="nav-icon" aria-hidden="true">☷</span>
                            Lehrgänge
                        </SidebarGroupButton>

                        {activeGroup === 'lehrgaenge' && (
                            <div className="sidebar-subnav">
                                <SidebarLink to="/trainingdashboard?level=gemeinde" currentPath={currentPath}>
                                    <span className="nav-icon" aria-hidden="true">⌂</span>
                                    Gemeindeebene
                                </SidebarLink>
                                <SidebarLink to="/trainingdashboard?level=kreis" currentPath={currentPath}>
                                    <span className="nav-icon" aria-hidden="true">⌂</span>
                                    Kreisebene
                                </SidebarLink>
                                <SidebarLink to="/trainingdashboard?level=land" currentPath={currentPath}>
                                    <span className="nav-icon" aria-hidden="true">▣</span>
                                    Landesebene
                                </SidebarLink>
                            </div>
                        )}
                            </>
                        )}

                        <SidebarGroupButton
                            isActive={activeGroup === 'verwaltung'}
                            onClick={(to) => {
                                setActiveGroup(activeGroup === 'verwaltung' ? null : 'verwaltung');

                                if (to) {
                                    navigate(to);
                                }
                            }}
                        >
                            <span className="nav-icon" aria-hidden="true">⚙</span>
                            Verwaltung
                        </SidebarGroupButton>

                        {activeGroup === 'verwaltung' && (
                            <div className="sidebar-subnav">
                                <SidebarLink to="/firedepartments" currentPath={currentPath}>
                                    <span className="nav-icon" aria-hidden="true">⌂</span>
                                    Feuerwehren
                                </SidebarLink>
                                {canManageUsers && (
                                    <SidebarLink to="/users" currentPath={currentPath}>
                                        <span className="nav-icon" aria-hidden="true">♙</span>
                                        Benutzer
                                    </SidebarLink>
                                )}
                            </div>
                        )}
                    </>
                ) : (
                    <></>
                )}
            </nav>
        </aside>
    );
}

function getActiveGroup(currentPath: string): SidebarGroup | null {
    if (currentPath === '/trainingdashboard' || currentPath.startsWith('/trainingdashboard?level=')) {
        return 'lehrgaenge';
    }

    if (
        currentPath === '/hydrant' ||
        currentPath === '/firedepartments' ||
        currentPath.startsWith('/trainingdashboard?view=') ||
        currentPath.startsWith('/dashboard?view=')
    ) {
        return 'verwaltung';
    }

    return null;
}

function SidebarGroupButton(props: {
    isActive: boolean;
    to?: string;
    onClick: (to?: string) => void;
    children: ReactNode;
}) {
    return (
        <button
            type="button"
            className={`sidebar-group-button${props.isActive ? ' active' : ''}`}
            aria-expanded={props.isActive}
            onClick={() => props.onClick(props.to || undefined)}
        >
            <span className="sidebar-group-button__content">{props.children}</span>
            <span className="sidebar-group-button__chevron" aria-hidden="true">
                {props.isActive ? '⌃' : '⌄'}
            </span>
        </button>
    );
}

function SidebarLink(props: {
    to: string;
    currentPath: string;
    children: ReactNode;
}) {
    return (
        <NavLink
            to={props.to}
            className={props.currentPath === props.to ? 'active' : undefined}
        >
            {props.children}
        </NavLink>
    );
}
