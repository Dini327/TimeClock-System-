import { Navigate, Outlet } from 'react-router-dom';
import type { StoredUser, UserRole } from '../../types';

interface Props {
  requiredRole?: UserRole;
}

function ProtectedRoute({ requiredRole }: Props) {
  const token = localStorage.getItem('token');

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole) {
    const stored = localStorage.getItem('user');
    const user: StoredUser | null = stored ? (JSON.parse(stored) as StoredUser) : null;
    if (!user || user.role !== requiredRole) {
      return <Navigate to="/dashboard" replace />;
    }
  }

  return <Outlet />;
}

export default ProtectedRoute;
