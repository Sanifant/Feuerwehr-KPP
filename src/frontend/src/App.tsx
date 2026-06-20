import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import './App.css';
import HydrantManager from './Components/HydrantManager';
import ProtectedRoute from './PrtoectedRoute';
import Navbar from './Navbar';
import Home from './Components/Home';
import { AuthProvider } from './AuthContext';


const App: React.FC = () => {
    return (
        <AuthProvider>
            <BrowserRouter>
                <Navbar />
                <Routes>
                    {/* Public Routes */}
                    <Route path="/" element={<Home />} />

                    {/* Protected Routes */}
                    <Route element={<ProtectedRoute />}>
                        <Route path="/hydrant" element={<HydrantManager />} />
                    </Route>
                </Routes>
            </BrowserRouter>
        </AuthProvider>
    );
};

export default App;
