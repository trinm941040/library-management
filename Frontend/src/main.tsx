import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './auth/AuthProvider'
import { SettingsProvider } from './settings/SettingsProvider'
import { ToastProvider } from './common/components'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <BrowserRouter>
    <SettingsProvider>
      <ToastProvider position="top-right">
        <AuthProvider>
          <App />
        </AuthProvider>
      </ToastProvider>
    </SettingsProvider>
  </BrowserRouter>,
)
