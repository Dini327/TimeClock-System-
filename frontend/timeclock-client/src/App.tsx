import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from './components/common/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import EmployeeDashboardPage from './pages/EmployeeDashboardPage';
import AdminPage from './pages/AdminPage';

function App() {
  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<LoginPage />} />

      {/* Any authenticated user */}
      <Route element={<ProtectedRoute />}>
        <Route path="/dashboard" element={<EmployeeDashboardPage />} />
      </Route>

      {/* Admin only */}
      <Route element={<ProtectedRoute requiredRole="Admin" />}>
        <Route path="/admin" element={<AdminPage />} />
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}

export default App;
