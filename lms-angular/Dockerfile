# syntax=docker/dockerfile:1.6
# Dev-mode Angular container. Runs `ng serve` with the watch + HMR pipeline so
# edits on the host (bind-mounted into /app/src) reload in the browser.

FROM node:20-alpine AS dev
WORKDIR /app

# Install dependencies in their own layer for cache reuse.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

# Project sources. In docker-compose `src/` is overridden by a bind-mount so
# host edits propagate; the rest of these files (angular.json, tsconfig*) make
# the image self-sufficient if you run it without compose.
COPY angular.json tsconfig.json tsconfig.app.json tsconfig.spec.json ./
COPY public/ public/
COPY src/ src/

EXPOSE 4200

# `--host 0.0.0.0` is required so the dev server is reachable from outside the
# container. `--poll 1000` makes the watcher work reliably with bind-mounts on
# Windows + WSL2.
CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--port", "4200", "--poll", "1000"]
