# OpenHdWebUi

Package name: open-hd-web-ui \
Exec name: TODO \
Service name: openhd-web-ui \

## Serving the full OpenHD documentation locally

The Debian package produced by this repository automatically bundles a fresh static build of the official [OpenHD Docusaurus site](https://github.com/OpenHD/OpenHD-Website). The files are installed under `/usr/local/share/openhd/docs` and the application comes preconfigured to expose them at `/docs`, so the documentation remains available even without an internet connection.

If you want to rebuild the documentation manually (for example while developing locally) you can follow the same steps that the packaging process performs:

1. Clone the documentation repository somewhere on the device:

   ```bash
   git clone https://github.com/OpenHD/OpenHD-Website.git /usr/local/share/openhd/OpenHD-Website
   ```

2. Build the static site with Node.js. The OpenHD website recommends pnpm, but any Node.js package manager will work. Override the base URL so the generated site is ready to be served from `/docs`:

   ```bash
   cd /usr/local/share/openhd/OpenHD-Website
   npm install
   BASE_URL=/docs/ npm run build
   ```

   The build output will be written to the `build` directory inside the clone. Copy the contents of that directory to `/usr/local/share/openhd/docs` and ensure the directory remains readable by the `openhd-web-ui` service user.

3. Restart the `openhd-web-ui` service. The **Docs** button on the front page will now load the refreshed local clone. If the local files are missing or become unavailable the UI automatically falls back to the public website.
