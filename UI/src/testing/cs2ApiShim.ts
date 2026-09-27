interface ShimBinding<T> {
  value: T;
  listeners: Set<(value: T) => void>;
}

const bindings = new Map<string, ShimBinding<any>>();

export interface ValueBinding<T> {
  readonly value: T;
  subscribe(listener: (value: T) => void): { dispose(): void };
  dispose(): void;
}

export function bindValue<T>(group: string, name: string, fallbackValue?: T): ValueBinding<T> {
  const key = `${group}.${name}`;
  let binding = bindings.get(key) as ShimBinding<T> | undefined;
  if (!binding) {
    binding = { value: fallbackValue as T, listeners: new Set() };
    bindings.set(key, binding);
  }
  const current = binding;
  return {
    get value() {
      return current.value;
    },
    subscribe(listener) {
      current.listeners.add(listener);
      return { dispose: () => current.listeners.delete(listener) };
    },
    dispose() {
      current.listeners.clear();
    },
  };
}

export function trigger(_group: string, _name: string, ..._args: unknown[]): void {
  // Test runs validate the binding contract without a live Gameface host.
}

export function useValue<T>(binding: ValueBinding<T>): T {
  return binding.value;
}
