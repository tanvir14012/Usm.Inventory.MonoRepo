# =============================================================================
# Frontend Dockerfile — Angular + Nginx on Alpine Linux
# =============================================================================
# Stage 1  : Build Angular app with Node.js
# Stage 2  : Serve the static output with nginx:alpine
#
# SSL certs (server.crt / server.key / ca.crt) are NOT baked into the image;
# they are mounted as read-only volumes by docker-compose at runtime.
# =============================================================================

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM node:22-alpine AS build

WORKDIR /app

# Copy only package manifests first to leverage Docker layer caching
COPY Frontend/Angular/package.json Frontend/Angular/package-lock.json ./
RUN npm ci --prefer-offline --no-audit --no-fund

# Copy the full Angular source and build for production
# outputHashing:all is set in angular.json — all output files (JS, CSS, images
# processed through the pipeline) will have a content-hash (MD5/xxHash) in
# their filename, enabling long-lived immutable CDN caching in nginx.
COPY Frontend/Angular/ ./
RUN npm run build -- --configuration production

# ── Stage 2: Serve ────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine

# Install OpenSSL (for certificate utilities) and ca-certificates
RUN apk add --no-cache openssl ca-certificates

# Create required directories
RUN mkdir -p /etc/nginx/ssl \
             /var/cache/nginx/api \
             /var/log/nginx

# Copy Angular build output (dist/<project>/browser for @angular/build:application)
COPY --from=build /app/dist/Angular/browser /usr/share/nginx/html

# Copy nginx configuration
COPY Frontend/nginx/nginx.conf /etc/nginx/nginx.conf

# SSL certificates are mounted at runtime via docker-compose:
#   /etc/nginx/ssl/server.crt
#   /etc/nginx/ssl/server.key
#   /etc/nginx/ssl/ca.crt

EXPOSE 80 443

CMD ["nginx", "-g", "daemon off;"]
