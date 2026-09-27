import { bindValue, trigger, useValue } from "cs2/api";
import {
  DEFAULT_ASSET_QUERY_STATE,
  EMPTY_UI_SNAPSHOT,
  MAX_ASSET_PAGE_SIZE,
  type AssetAuditorBindings,
  type AssetQueryRequest,
  type AssetQueryState,
  type CountKind,
  type UiObservation,
  type UiScanOptions,
  type UiSnapshot,
} from "./types";

export const UI_BINDING_GROUP = "CS2AssetPerformanceAuditor";

const snapshotBinding = bindValue<string>(UI_BINDING_GROUP, "snapshot", "{}");
const exportedReportBinding = bindValue<string>(UI_BINDING_GROUP, "exportedReport", "");

export function useAuditorSnapshot(): UiSnapshot {
  return parseSnapshot(useValue(snapshotBinding));
}

export function useExportedReport(): string {
  return useValue(exportedReportBinding);
}

export const nativeBindings: AssetAuditorBindings = {
  requestCensus(options: UiScanOptions): void {
    trigger(UI_BINDING_GROUP, "requestCensus", JSON.stringify(options));
  },
  cancelCensus(): void {
    trigger(UI_BINDING_GROUP, "cancelCensus");
  },
  requestAssetsPage(query: AssetQueryRequest): void {
    trigger(UI_BINDING_GROUP, "queryAssets", JSON.stringify(query));
  },
  requestExport(): void {
    trigger(UI_BINDING_GROUP, "requestExport");
  },
  updateSettings(options: UiScanOptions): void {
    trigger(UI_BINDING_GROUP, "updateSettings", JSON.stringify(options));
  },
};

export function createAssetQuery(state: AssetQueryState): AssetQueryRequest {
  return {
    searchText: state.searchText.trim(),
    traitFilter: state.traitFilter,
    sourceFilter: state.sourceFilter,
    presenceFilter: state.presenceFilter,
    sort: state.sort,
    offset: Math.max(0, Math.floor(state.offset)),
    limit: Math.min(MAX_ASSET_PAGE_SIZE, Math.max(1, Math.floor(state.pageSize))),
  };
}

export function updateAssetQueryState(
  current: AssetQueryState,
  patch: Partial<AssetQueryState>,
): AssetQueryState {
  const changesFilter = Object.keys(patch).some(
    (key) => key !== "offset" && key !== "pageSize",
  );
  return {
    ...current,
    ...patch,
    offset: patch.offset ?? (changesFilter ? 0 : current.offset),
  };
}

export function formatObservation(observation: UiObservation<number>): string {
  switch (observation.availability) {
    case "Available":
      return observation.value === null || observation.value === undefined
        ? "Unknown"
        : String(observation.value);
    case "NotScanned":
      return "Not scanned";
    case "NotApplicable":
      return "N/A";
    case "Unsupported":
      return "Unsupported";
    case "Failed":
      return "Failed";
    default:
      return "Unknown";
  }
}

export function formatCountKind(countKind: CountKind): string {
  switch (countKind) {
    case "TopLevelObjects":
      return "Top-level objects";
    case "SubordinateObjects":
      return "Subordinate objects";
    case "LiveObjectReferences":
      return "Live object references";
    case "NetworkEdges":
      return "Network edges";
    default:
      return "Not applicable";
  }
}

export function createEscapeCloseHandler(onClose: () => void) {
  return (event: Pick<KeyboardEvent, "key" | "preventDefault">): void => {
    if (event.key !== "Escape") return;
    event.preventDefault();
    onClose();
  };
}

function parseSnapshot(raw: string): UiSnapshot {
  try {
    const value: unknown = JSON.parse(raw);
    if (value && typeof value === "object" && "scanStatus" in value) {
      return value as UiSnapshot;
    }
  } catch {
    // A malformed/missing binding keeps the documented empty view available.
  }
  return EMPTY_UI_SNAPSHOT;
}

export { DEFAULT_ASSET_QUERY_STATE };
