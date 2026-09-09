import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import ProtectedRoute from './components/ProtectedRoute'
import { AuthProvider } from './context/AuthContext'
import { ToastProvider } from './context/ToastContext'
import AdminPage from './pages/AdminPage'
import { LoginPage, RegisterPage } from './pages/AuthPages'
import CreateListingPage from './pages/CreateListingPage'
import HomePage from './pages/HomePage'
import ListingDetailPage from './pages/ListingDetailPage'
import ListingsPage from './pages/ListingsPage'
import NotFoundPage from './pages/NotFoundPage'
import ProfilePage from './pages/ProfilePage'
import PublicProfilePage from './pages/PublicProfilePage'

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <Routes>
            <Route element={<Layout />}>
              {/* Ochiq sahifalar */}
              <Route index element={<HomePage />} />
              <Route path="listings" element={<ListingsPage />} />
              <Route path="listings/:id" element={<ListingDetailPage />} />
              <Route path="u/:username" element={<PublicProfilePage />} />
              <Route path="login" element={<LoginPage />} />
              <Route path="register" element={<RegisterPage />} />

              {/* Avtorizatsiya talab qiladigan sahifalar */}
              <Route element={<ProtectedRoute />}>
                <Route path="profile" element={<ProfilePage />} />
                <Route path="create" element={<CreateListingPage />} />
              </Route>

              {/* Moderator */}
              <Route element={<ProtectedRoute moderatorOnly />}>
                <Route path="admin" element={<AdminPage />} />
              </Route>

              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}
