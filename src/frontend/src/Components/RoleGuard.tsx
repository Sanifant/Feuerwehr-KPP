import React from 'react';
import { useRole } from '../hooks/useRole';

interface RoleGuardProps {
    roles: string[];
    children: React.ReactNode;
    fallback?: React.ReactNode;
}

/**
 * Component to conditionally render children based on user roles
 * @param roles - Array of role names that are allowed to see the content
 * @param children - Content to render if user has any of the specified roles
 * @param fallback - Optional content to render if user doesn't have required roles
 */
export const RoleGuard: React.FC<RoleGuardProps> = ({ roles, children, fallback = null }) => {
    const { hasRole } = useRole();

    const hasRequiredRole = roles.some(role => hasRole(role));

    if (!hasRequiredRole) {
        return <>{fallback}</>;
    }

    return <>{children}</>;
};

export default RoleGuard;
