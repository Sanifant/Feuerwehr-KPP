import { useAuth } from '../AuthContext';

export const useRole = () => {
    const { user, hasRole } = useAuth();

    return {
        user,
        hasRole,
        isAdmin: hasRole('Admin'),
        isCommander: hasRole('Commander'),
        isFirefighter: hasRole('Firefighter'),
        isViewer: hasRole('Viewer'),
        canViewHydrants: hasRole('Admin') || hasRole('Commander') || hasRole('Firefighter'),
        canManageHydrants: hasRole('Admin') || hasRole('Commander'),
        canManageTraining: hasRole('Admin') || hasRole('Commander'),
        canManageUsers: hasRole('Admin'),
    };
};
