import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import HomePage from './pages/HomePage';
import BuilderDashboard from './pages/BuilderDashboard';
import ModelManager from './pages/ModelManager';
import './App.css';

const App: React.FC = () => {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/builder" element={<BuilderDashboard />} />
                <Route path="/models" element={<ModelManager />} />
            </Routes>
        </Router>
    );
};

export default App;
