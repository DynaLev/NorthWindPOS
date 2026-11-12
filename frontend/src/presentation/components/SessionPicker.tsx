import type { PosSession } from '../../domain/models'

interface SessionPickerProps {
  sessions: PosSession[]
  selectedId: number | undefined
  onSelect: (id: number) => void
}

/** Muestra la sesión POS en uso y permite elegir otra si hay varias abiertas (RF04). */
export function SessionPicker({ sessions, selectedId, onSelect }: SessionPickerProps) {
  if (sessions.length === 0) return <p className="muted">Sin sesión POS abierta en Odoo.</p>
  const selected = sessions.find((s) => s.id === selectedId) ?? sessions[0]
  const cash = selected.cashPaymentMethodName ?? 'sin método de efectivo'

  if (sessions.length === 1) {
    return <p className="muted">Sesión {selected.name} · {selected.configName} · efectivo: {cash}</p>
  }
  return (
    <label className="session-picker">
      Sesión POS
      <select value={selected.id} onChange={(e) => onSelect(Number(e.target.value))}>
        {sessions.map((s) => (
          <option key={s.id} value={s.id}>
            {s.name} · {s.configName}
          </option>
        ))}
      </select>
      <span className="muted"> Efectivo: {cash}</span>
    </label>
  )
}
