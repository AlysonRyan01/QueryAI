# syntax=docker/dockerfile:1
FROM node:24-alpine AS build
ARG VERSION=0.1.0
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN VITE_APP_VERSION=${VERSION} npm run build

FROM nginxinc/nginx-unprivileged:alpine AS runtime
ARG VERSION=0.1.0
LABEL org.opencontainers.image.title="QueryAI Web" \
      org.opencontainers.image.version="${VERSION}"
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/ >/dev/null || exit 1
