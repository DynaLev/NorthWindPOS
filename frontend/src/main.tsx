import { BrowserRouter } from 'react-router-dom'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './presentation/App'
import { ServicesProvider } from './presentation/ServicesContext'
import './pos.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ServicesProvider>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </ServicesProvider>
  </StrictMode>,
)
