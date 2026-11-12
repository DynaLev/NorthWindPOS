/** Error de la API .NET con su código HTTP y código de negocio (ProblemDetails.title). */
export class ApiError extends Error {
  readonly status: number
  readonly code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

export function describeError(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback
}

/** Resultado incierto (504 / SALE_RESULT_UNCERTAIN): hay que reintentar con el MISMO id. */
export function isUncertain(error: unknown): boolean {
  return error instanceof ApiError && (error.code === 'SALE_RESULT_UNCERTAIN' || error.status === 504 || error.status === 0)
}
