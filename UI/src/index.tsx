import React from "react";
import type { ModRegistrar } from "cs2/modding";
import assetAuditorStyles from "./assetAuditor.module.scss?raw";
import { AssetAuditorRoot } from "./AssetAuditorRoot";

export function AssetAuditorModule(): React.JSX.Element {
  return (
    <>
      <style>{assetAuditorStyles}</style>
      <AssetAuditorRoot />
    </>
  );
}

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("GameTopLeft", AssetAuditorModule);
};

export default register;
