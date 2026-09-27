export type Availability =
  | "Available"
  | "NotScanned"
  | "NotApplicable"
  | "Unsupported"
  | "Failed";

export type CountKind =
  | "None"
  | "TopLevelObjects"
  | "SubordinateObjects"
  | "LiveObjectReferences"
  | "NetworkEdges";

export type CensusPresence =
  | "Present"
  | "NotPresentAtSnapshot"
  | "NotApplicable"
  | "Unknown";

export interface UiObservation<T = number> {
  availability: Availability;
  value?: T | null;
  origin?: string;
  capturedAt?: string;
  diagnosticCode?: string | null;
}

export interface AssetRow {
  prefabId: string;
  prefabType: string;
  displayName: string;
  sourceLabel: string;
  traits: string[];
  countKind: CountKind;
  instances: UiObservation<number>;
  presence: CensusPresence;
  counters?: {
    topLevelObjects: UiObservation<number>;
    subordinateObjects: UiObservation<number>;
    liveObjectReferences: UiObservation<number>;
    networkEdges: UiObservation<number>;
  };
}

export interface AssetPage {
  offset: number;
  limit: number;
  totalCount: number;
  items: AssetRow[];
}

export type SourceFilter =
  | "Any"
  | "Builtin"
  | "SubscribedMod"
  | "Packaged"
  | "UserProvided"
  | "Unknown";

export type AssetSort =
  | "DisplayNameAscending"
  | "DisplayNameDescending"
  | "PrefabIdAscending"
  | "InstancesDescending";

export interface AssetQueryState {
  searchText: string;
  traitFilter: string | null;
  sourceFilter: SourceFilter;
  presenceFilter: CensusPresence | null;
  sort: AssetSort;
  offset: number;
  pageSize: number;
}

export interface AssetQueryRequest {
  searchText: string;
  traitFilter: string | null;
  sourceFilter: SourceFilter;
  presenceFilter: CensusPresence | null;
  sort: AssetSort;
  offset: number;
  limit: number;
}

export interface ScanStatusData {
  state: "Idle" | "Running" | "CancellationRequested" | "Cancelled" | "Failed" | "Completed";
  stage: string;
  stageNumber: number;
  totalStages: number;
  completedItems: number | null;
  totalItems: number | null;
}

export interface UiScanOptions {
  collectSubordinateObjects: boolean;
  collectNetworkEdges: boolean;
}

export interface UiCensusCounts {
  topLevelObjects: UiObservation<number>;
  subordinateObjects: UiObservation<number>;
  liveObjectReferences: UiObservation<number>;
  networkEdges: UiObservation<number>;
}

export interface UiSummary {
  gameVersion: string;
  modVersion: string;
  compatibility: string;
  capabilities: Array<{ id: string; state: string; detail?: string | null }>;
  catalogCount: number;
  catalogGeneration: number;
  catalogCapturedAt: string | null;
  censusWasScanned: boolean;
  censusCapturedAt: string | null;
  censusCatalogGeneration: number | null;
  censusMatchesCatalog: boolean;
  queryProfileVersion: string | null;
  censusCounts: UiCensusCounts;
}

export interface UiSnapshot {
  scanStatus: ScanStatusData;
  summary: UiSummary;
  assetPage: AssetPage;
  settings: UiScanOptions;
}

export interface AssetAuditorBindings {
  requestCensus(options: UiScanOptions): void;
  cancelCensus(): void;
  requestAssetsPage(query: AssetQueryRequest): void;
  requestExport(): void;
  updateSettings(options: UiScanOptions): void;
}

export const MAX_ASSET_PAGE_SIZE = 200;

export const DEFAULT_ASSET_QUERY_STATE: AssetQueryState = {
  searchText: "",
  traitFilter: null,
  sourceFilter: "Any",
  presenceFilter: null,
  sort: "DisplayNameAscending",
  offset: 0,
  pageSize: 100,
};

export const DEFAULT_SCAN_OPTIONS: UiScanOptions = {
  collectSubordinateObjects: true,
  collectNetworkEdges: true,
};

export const EMPTY_OBSERVATION: UiObservation<number> = {
  availability: "NotScanned",
  value: null,
};

export const EMPTY_UI_SNAPSHOT: UiSnapshot = {
  scanStatus: {
    state: "Idle",
    stage: "Idle",
    stageNumber: 0,
    totalStages: 8,
    completedItems: null,
    totalItems: null,
  },
  summary: {
    gameVersion: "Unknown",
    modVersion: "0.1.0",
    compatibility: "Untested",
    capabilities: [],
    catalogCount: 0,
    catalogGeneration: 0,
    catalogCapturedAt: null,
    censusWasScanned: false,
    censusCapturedAt: null,
    censusCatalogGeneration: null,
    censusMatchesCatalog: false,
    queryProfileVersion: null,
    censusCounts: {
      topLevelObjects: EMPTY_OBSERVATION,
      subordinateObjects: EMPTY_OBSERVATION,
      liveObjectReferences: EMPTY_OBSERVATION,
      networkEdges: EMPTY_OBSERVATION,
    },
  },
  assetPage: { offset: 0, limit: 100, totalCount: 0, items: [] },
  settings: DEFAULT_SCAN_OPTIONS,
};
