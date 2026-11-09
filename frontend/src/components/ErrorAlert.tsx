import React from 'react'

interface ErrorAlertProps {
  message?: string
}

export const ErrorAlert: React.FC<ErrorAlertProps> = ({ message }) => {
  if (!message) return null
  return (
    <div role="alert" aria-live="polite" style={{ color: '#b00020', padding: '8px' }}>
      {message}
    </div>
  )
}
