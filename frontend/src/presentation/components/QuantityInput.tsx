import { useState } from 'react'

interface QuantityInputProps {
  label: string
  quantity: number
  onCommit: (quantity: number) => void
}

/**
 * Campo numérico de cantidad (RF03). Permite borrar y escribir libremente; aplica la cantidad
 * cuando es un número mayor que 0. Al salir del campo, 0 quita la línea y un valor vacío se restaura.
 */
export function QuantityInput({ label, quantity, onCommit }: QuantityInputProps) {
  const [text, setText] = useState(String(quantity))
  const [previous, setPrevious] = useState(quantity)
  if (previous !== quantity) {
    setPrevious(quantity)
    setText(String(quantity))
  }

  return (
    <label>
      <span className="sr-only">{label}</span>
      <input
        type="number"
        min={0}
        step={1}
        inputMode="decimal"
        value={text}
        onChange={(e) => {
          setText(e.target.value)
          const value = e.target.valueAsNumber
          if (Number.isFinite(value) && value > 0) onCommit(value)
        }}
        onBlur={() => {
          const value = Number(text)
          if (text.trim() === '' || !Number.isFinite(value)) setText(String(quantity))
          else if (value <= 0) onCommit(0)
        }}
      />
    </label>
  )
}
