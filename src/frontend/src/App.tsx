import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import './App.css';
import HydrantManager from './Components/HydrantManager';
import ProtectedRoute from './ProtectedRoute';
import Navbar from './Navbar';
import Home from './Components/Home';
import { AuthProvider } from './AuthContext';
import { TrainingDashboard } from './Components/TrainingDashboard.tsx';
import Login from './Components/Login';
import { SideBar } from './SideBar';
import FireDepartmentController from './Components/FireDepartmentController';
import UserManagement from './Components/UserManagement';
import { Dashboard } from './Components/Dashboard.tsx';
import { ChangePassword } from './Components/ChangePassword.tsx';


const App: React.FC = () => {
    return (
        <AuthProvider>
            <BrowserRouter>
                <Navbar />
                <div className="dashboard-layout">
                    <SideBar />
                    <main className="dashboard">
                        <Routes>
                            {/* Public Routes */}
                            <Route path="/" element={<Home />} />
                            <Route path="/login" element={<Login />} />
                            <Route path="/password/:token" element={<ChangePassword />} />


                            {/* Protected Routes */}
                            <Route element={<ProtectedRoute />}>
                                <Route path="/dashboard" element={<Dashboard />} />
                                <Route path="/firedepartments" element={<FireDepartmentController />} />
                                <Route path="/hydrant" element={<HydrantManager />} />
                                <Route path="/trainingdashboard" element={<TrainingDashboard />} />
                                <Route path="/users" element={<UserManagement />} />
                            </Route>
                        </Routes>
                    </main>
                </div>
            </BrowserRouter>
        </AuthProvider>
    );
};

export default App;
