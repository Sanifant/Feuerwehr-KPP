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
import IncidentMapPage from './Components/IncidentMapPage.tsx';
import IncidentDiaryPage from './Components/IncidentDiaryPage.tsx';
import DiaryCategoryAdmin from './Components/DiaryCategoryAdmin.tsx';
import IncidentOverviewPage from './Components/IncidentOverviewPage.tsx';
import ActiveIncidentProvider from './ActiveIncidentProvider.tsx';
import ActiveIncidentRoute from './Components/ActiveIncidentRoute.tsx';
import IncidentArchivePage from './Components/IncidentArchivePage.tsx';
import MapSymbolAdmin from './Components/MapSymbolAdmin.tsx';
import InventoryPage from './Components/InventoryPage.tsx';
import EquipmentTypesPage from './Components/EquipmentTypesPage.tsx';
import InventoryCategoryAdmin from './Components/InventoryCategoryAdmin.tsx';
import VehiclesPage from './Components/VehiclesPage.tsx';
import VehicleTypesPage from './Components/VehicleTypesPage.tsx';
import VehicleStatusesPage from './Components/VehicleStatusesPage.tsx';


const App: React.FC = () => {
    return (
        <AuthProvider>
            <BrowserRouter>
                <ActiveIncidentProvider>
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
                                    <Route path="/password" element={<ChangePassword />} />
                                    <Route path="/dashboard" element={<Dashboard />} />
                                    <Route path="/firedepartments" element={<FireDepartmentController />} />
                                    <Route path="/hydrant" element={<HydrantManager />} />
                                    <Route path="/trainingdashboard" element={<TrainingDashboard />} />
                                    <Route path="/users" element={<UserManagement />} />
                                    <Route path="/incidents" element={<IncidentOverviewPage />} />
                                    <Route path="/incident-archive" element={<IncidentArchivePage />} />
                                    <Route element={<ActiveIncidentRoute />}>
                                        <Route path="/incident-map" element={<IncidentMapPage />} />
                                        <Route path="/incident-diary" element={<IncidentDiaryPage />} />
                                    </Route>
                                    <Route path="/diary-categories" element={<DiaryCategoryAdmin />} />
                                    <Route path="/map-symbols" element={<MapSymbolAdmin />} />
                                    <Route path="/inventory" element={<InventoryPage />} />
                                    <Route path="/inventory/types" element={<EquipmentTypesPage />} />
                                    <Route path="/inventory/categories" element={<InventoryCategoryAdmin />} />
                                    <Route path="/vehicles" element={<VehiclesPage />} />
                                    <Route path="/vehicles/manage" element={<VehiclesPage management />} />
                                    <Route path="/vehicle-types" element={<VehicleTypesPage />} />
                                    <Route path="/vehicle-statuses" element={<VehicleStatusesPage />} />
                                </Route>
                            </Routes>
                        </main>
                    </div>
                </ActiveIncidentProvider>
            </BrowserRouter>
        </AuthProvider>
    );
};

export default App;
