import { useEffect, useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from './AuthContext';
import { useRole } from './hooks/useRole';
import { useActiveIncident } from './hooks/useActiveIncident';

type SidebarGroup = 'lehrgaenge' | 'inventory' | 'verwaltung' | 'einsatz';

export function SideBar() {
    const { isAuthenticated } = useAuth();
    const { canManageUsers, canViewHydrants, canManageTraining, canViewSituationMap, canViewIncidentDiary, isAdmin } = useRole();
    const { incident, loading, error } = useActiveIncident();
    const incidentToolsDisabled = loading || !!error || !incident;
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

                        {canViewHydrants && (
                            <SidebarLink to="/hydrant" currentPath={currentPath}>
                                <span className="nav-icon" aria-hidden="true"></span>
                                Hydrant
                            </SidebarLink>
                        )}

                        {(isAdmin || canViewSituationMap || canViewIncidentDiary) && (
                            <>
                            <SidebarGroupButton
                                isActive={activeGroup === 'einsatz'}
                                onClick={(to) => {
                                    setActiveGroup(activeGroup === 'einsatz' ? null : 'einsatz');

                                    if (to) {
                                        navigate(to);
                                    }
                                }}
                            >
                                <span className="nav-icon" aria-hidden="true">☷</span>
                                Einsätze                            
                            </SidebarGroupButton>

                            {activeGroup === 'einsatz' && (
                                <div className="sidebar-subnav">
                                    {(canViewSituationMap || canViewIncidentDiary) && <SidebarLink to="/incidents" currentPath={currentPath}>
                                        <span className="nav-icon" aria-hidden="true">🗺</span>
                                        Einsatzübersicht
                                    </SidebarLink>}
                                    {canViewSituationMap && (
                                        <SidebarLink to="/incident-map" currentPath={currentPath} disabled={incidentToolsDisabled}>
                                            <span className="nav-icon" aria-hidden="true">🗺</span>
                                            Lagekarte
                                        </SidebarLink>
                                    )}

                                    {canViewIncidentDiary && (
                                        <SidebarLink to="/incident-diary" currentPath={currentPath} disabled={incidentToolsDisabled}>
                                            <span className="nav-icon" aria-hidden="true">📝</span>
                                            Einsatztagebuch
                                        </SidebarLink>
                                    )}
                                    {isAdmin && <SidebarLink to="/incident-archive" currentPath={currentPath}>
                                        <span className="nav-icon" aria-hidden="true">▤</span>
                                        Einsatzarchiv
                                    </SidebarLink>}
                                </div>
                            )}
                            </>

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
                                {canManageUsers && (
                                    <SidebarLink to="/users" currentPath={currentPath}>
                                        <span className="nav-icon" aria-hidden="true">♙</span>
                                        Benutzer
                                    </SidebarLink>
                                )}
                                {isAdmin && (
                                    <SidebarLink to="/firedepartments" currentPath={currentPath}>
                                        <span className="nav-icon" aria-hidden="true">⌂</span>
                                        Feuerwehren
                                    </SidebarLink>
                                )}
                                {isAdmin && (
                                    <SidebarLink to="/diary-categories" currentPath={currentPath}>
                                        <span className="nav-icon" aria-hidden="true">☰</span>
                                        Tagebuchkategorien
                                    </SidebarLink>
                                )}
                                {isAdmin && <SidebarLink to="/map-symbols" currentPath={currentPath}>
                                    <span className="nav-icon" aria-hidden="true">◇</span>
                                    Lagekartensymbole
                                </SidebarLink>}
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

    if (['/incidents', '/incident-map', '/incident-diary', '/incident-archive'].includes(currentPath.split('?')[0])) {
        return 'einsatz';
    }

    if (
        currentPath === '/hydrant' ||
        currentPath === '/diary-categories' ||
        currentPath === '/map-symbols' ||
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
    disabled?: boolean;
    children: ReactNode;
}) {
    if (props.disabled) return <span className="sidebar-link-disabled" role="link" aria-disabled="true"
        title="Erst mit einem aktiven Einsatz verfügbar.">{props.children}</span>;
    return (
        <NavLink
            to={props.to}
            className={props.currentPath === props.to ? 'active' : undefined}
        >
            {props.children}
        </NavLink>
    );
}
