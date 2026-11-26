import { render } from '@testing-library/react'
import { Provider } from 'react-redux'
import App from '@/App'
import { makeStore } from '@/app/store'

// Renders the whole app with a fresh store, starting at the given URL.
export function renderApp(path = '/') {
  window.history.replaceState({}, '', path)
  const store = makeStore()
  render(
    <Provider store={store}>
      <App />
    </Provider>,
  )
  return { store }
}
