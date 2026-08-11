import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../context/auth-context'
import { PageLoader } from './ui'

export default function ProtectedRoute({ moderatorOnly = false }: { moderatorOnly?: boolean }) {
  const { user, loading, isModerator } = useAuth()
  const location = useLocation()

  if (loading) return <PageLoader />

  // Kirgandan keyin foydalanuvchi shu sahifaga qaytariladi.
  if (!user) return <Navigate to="/login" state={{ from: location.pathname }} replace />

  if (moderatorOnly && !isModerator) return <Navigate to="/" replace />

  return <Outlet />
}
