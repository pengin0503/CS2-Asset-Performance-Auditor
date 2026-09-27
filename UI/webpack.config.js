const path = require("node:path");

module.exports = {
  entry: "./src/index.tsx",
  experiments: { outputModule: true },
  output: {
    path: path.resolve(__dirname, "dist"),
    filename: "asset-auditor.js",
    clean: true,
    module: true,
    library: { type: "module" },
  },
  externalsType: "module",
  externals: { "cs2/api": "cs2/api" },
  resolve: {
    extensions: [".tsx", ".ts", ".js"],
  },
  module: {
    rules: [
      {
        test: /\.module\.scss$/,
        resourceQuery: /raw/,
        type: "asset/source",
      },
      {
        test: /\.tsx?$/,
        exclude: /node_modules/,
        use: {
          loader: "ts-loader",
          options: { transpileOnly: true },
        },
      },
    ],
  },
};
