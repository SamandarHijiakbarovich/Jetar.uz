import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import ErrorBoundary from './components/ErrorBoundary'
import './index.css'

/**
 * Sayt yangi versiyaga o'tganda, ochiq tabdagi eski JavaScript endi mavjud
 * bo'lmagan bo'laklarni (chunk) so'raydi va bu "qora ekran" ga olib keladi.
 * Vite bunday holatda `vite:preloadError` hodisasini yuboradi — uni ushlab,
 * sahifani bir marta avtomatik yangilaymiz (cheksiz sikldan saqlanamiz).
 */
const RELOAD_FLAG = 'jetar.chunk-reloaded'

window.addEventListener('vite:preloadError', (event) => {
  event.preventDefault()
  if (sessionStorage.getItem(RELOAD_FLAG)) return
  sessionStorage.setItem(RELOAD_FLAG, '1')
  window.location.reload()
})

// Muvaffaqiyatli yuklashdan keyin bayroqni tozalaymiz.
window.addEventListener('load', () => sessionStorage.removeItem(RELOAD_FLAG))

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </StrictMode>,
)
