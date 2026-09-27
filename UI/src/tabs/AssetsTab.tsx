import React from "react";
import { VirtualAssetTable } from "../components/VirtualAssetTable";
import {
  DEFAULT_ASSET_QUERY_STATE,
  type AssetPage,
  type AssetQueryState,
  type SourceFilter,
} from "../types";

interface AssetsTabProps {
  page: AssetPage;
  query: AssetQueryState;
  onQueryChange: (patch: Partial<AssetQueryState>) => void;
}

export function AssetsTab({ page, query, onQueryChange }: AssetsTabProps): React.JSX.Element {
  return (
    <section className="apa__tab-content" aria-labelledby="apa-assets-title">
      <div className="apa__section-heading">
        <div>
          <p className="apa__eyebrow">Bounded result window</p>
          <h2 id="apa-assets-title">Assets</h2>
        </div>
        <span className="apa__muted">{page.totalCount} matching Prefabs</span>
      </div>
      <div className="apa__filters">
        <label className="apa__field apa__field--search">
          <span>Search</span>
          <input
            type="search"
            value={query.searchText}
            placeholder="Name, Prefab ID, or source / pack"
            aria-label="Search assets"
            onChange={(event) => onQueryChange({ searchText: event.currentTarget.value })}
          />
        </label>
        <label className="apa__field">
          <span>Type</span>
          <select aria-label="Filter by type" value={query.traitFilter ?? "Any"} onChange={(event) => onQueryChange({ traitFilter: event.currentTarget.value === "Any" ? null : event.currentTarget.value })}>
            <option value="Any">Any type</option>
            <option value="Building">Building</option>
            <option value="ServiceBuilding">Service building</option>
            <option value="Prop">Prop</option>
            <option value="Tree">Tree</option>
            <option value="Vehicle">Vehicle</option>
            <option value="Network">Network</option>
          </select>
        </label>
        <label className="apa__field">
          <span>Source</span>
          <select aria-label="Filter by source" value={query.sourceFilter} onChange={(event) => onQueryChange({ sourceFilter: event.currentTarget.value as SourceFilter })}>
            <option value="Any">Any source</option>
            <option value="Builtin">Built-in</option>
            <option value="SubscribedMod">Subscribed mod</option>
            <option value="Packaged">Packaged</option>
            <option value="Unknown">Unknown</option>
          </select>
        </label>
        <label className="apa__field">
          <span>Presence</span>
          <select aria-label="Filter by presence" value={query.presenceFilter ?? "Any"} onChange={(event) => onQueryChange({ presenceFilter: event.currentTarget.value === "Any" ? null : event.currentTarget.value as NonNullable<AssetQueryState["presenceFilter"]> })}>
            <option value="Any">Any presence</option>
            <option value="Present">Present</option>
            <option value="NotPresentAtSnapshot">Not present</option>
            <option value="NotApplicable">Not applicable</option>
            <option value="Unknown">Unknown</option>
          </select>
        </label>
        <label className="apa__field">
          <span>Sort</span>
          <select aria-label="Sort assets" value={query.sort} onChange={(event) => onQueryChange({ sort: event.currentTarget.value as AssetQueryState["sort"] })}>
            <option value="DisplayNameAscending">Name A–Z</option>
            <option value="DisplayNameDescending">Name Z–A</option>
            <option value="PrefabIdAscending">Prefab ID</option>
            <option value="InstancesDescending">Instances</option>
          </select>
        </label>
      </div>
      <VirtualAssetTable
        page={page}
        onPageChange={(offset) => onQueryChange({ offset })}
      />
      <button type="button" className="apa__link-button" onClick={() => onQueryChange(DEFAULT_ASSET_QUERY_STATE)}>
        Reset filters
      </button>
    </section>
  );
}
