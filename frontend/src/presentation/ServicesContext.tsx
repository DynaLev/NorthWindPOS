import { createContext, useContext, type ReactNode } from 'react'
import { createDefaultServices, type Services } from '../infrastructure/container'

const defaultServices = createDefaultServices()
const ServicesContext = createContext<Services>(defaultServices)

export function ServicesProvider({ services, children }: { services?: Services; children: ReactNode }) {
  return <ServicesContext.Provider value={services ?? defaultServices}>{children}</ServicesContext.Provider>
}

// eslint-disable-next-line react/only-export-components
export function useServices(): Services {
  return useContext(ServicesContext)
}
