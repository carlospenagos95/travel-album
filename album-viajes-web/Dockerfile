# syntax=docker/dockerfile:1

FROM node:24-alpine AS build
WORKDIR /app

# package*.json primero para que npm ci quede cacheado mientras no cambien
# las dependencias.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.29-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
