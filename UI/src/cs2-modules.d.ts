declare module "cs2/api" {
  export interface ValueBinding<T> {
    readonly value: T;
    subscribe(listener: (value: T) => void): { dispose(): void };
    dispose(): void;
  }

  export function bindValue<T>(group: string, name: string, fallbackValue?: T): ValueBinding<T>;
  export function trigger(group: string, name: string, ...args: unknown[]): void;
  export function useValue<T>(binding: ValueBinding<T>): T;
}

declare module "cs2/modding" {
  export type AppendHookTargets = "Menu" | "Editor" | "Game" | "GameTopLeft" | "GameTopRight" | "GameBottomRight";
  export interface ModuleRegistry {
    append(target: AppendHookTargets, component: unknown, index?: number): void;
  }
  export type ModRegistrar = (moduleRegistry: ModuleRegistry) => void;
}

declare module "*.module.scss?raw" {
  const stylesheet: string;
  export default stylesheet;
}
