import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';

const ProtectedRoute: React.FC = () => {
    const { accessToken } = useAuth();

    /*
    if (!accessToken) {
        return <Navigate to="/login" replace />;
    }
    */
    return <Outlet />;
};

export default ProtectedRoute;