import React from "react";
import { createRoot } from "react-dom/client";
import { AssetAuditorRoot } from "./AssetAuditorRoot";

const mount = document.getElementById("root");
if (mount) {
  createRoot(mount).render(<AssetAuditorRoot />);
}
