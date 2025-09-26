# OpenHdWebUi

Package name: open-hd-web-ui \
Exec name: TODO \
Service name: openhd-web-ui \

## Serving the full OpenHD documentation locally

The web interface can expose a complete clone of the official [OpenHD Docusaurus site](https://github.com/OpenHD/OpenHD-Website) instead of the lightweight fallback that is bundled with the UI. This allows you to keep the documentation available on an offline air/ground unit while retaining the exact look-and-feel of the public website.

1. Clone the documentation repository somewhere on the device:

   ```bash
   git clone https://github.com/OpenHD/OpenHD-Website.git /usr/local/share/openhd/OpenHD-Website
   ```

2. Build the static site with Node.js (pnpm is recommended by the project). Because the web interface serves the documentation
   from the `/docs` sub-path, instruct Docusaurus to emit the site into `build/docs` by overriding the base URL:

   ```bash
   cd /usr/local/share/openhd/OpenHD-Website
   pnpm install
   BASE_URL=/docs/ pnpm build
   ```

   The build output will be written to the `build` directory inside the clone.

3. Point the web UI to the generated assets by editing `src/OpenHdWebUi.Server/appsettings.json` (or the corresponding deployment override) and set the `Documentation` section:

   ```json
   "Documentation": {
     "LocalDocsRoot": "/usr/local/share/openhd/OpenHD-Website/build/docs",
     "RequestPath": "/docs",
     "LocalIntroPage": "introduction/index.html"
   }
   ```

   Adjust `LocalIntroPage` if the introduction page lives under a different path in a future Docusaurus release.

4. Restart the `openhd-web-ui` service. The **Docs** button on the front page will now load the local clone. If the local files are missing or become unavailable the UI automatically falls back to the public website.
