import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { adManager } from './ads'

async function main() {
  await adManager.initialize();
  createRoot(document.getElementById('root')!).render(<App />)
}

main();
