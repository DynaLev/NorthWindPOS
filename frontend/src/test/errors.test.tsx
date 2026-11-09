import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ErrorAlert } from '../components/ErrorAlert'

describe('P08 - API error display', () => {
  it('shows error message on failure', () => {
    render(<ErrorAlert message="Error de conexión con el servidor" />)
    expect(screen.getByText(/Error de conexión/)).toBeInTheDocument()
  })
})
