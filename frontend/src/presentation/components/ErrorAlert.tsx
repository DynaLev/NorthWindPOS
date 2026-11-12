interface ErrorAlertProps {
  message?: string
}

/** Mensaje de error accesible (role="alert" ya implica aviso inmediato al lector de pantalla). */
export function ErrorAlert({ message }: ErrorAlertProps) {
  if (!message) return null
  return (
    <div role="alert" className="error">
      {message}
    </div>
  )
}
