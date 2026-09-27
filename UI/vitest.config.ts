import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    alias: {
      "cs2/api": new URL("./src/testing/cs2ApiShim.ts", import.meta.url).pathname,
    },
  },
});
