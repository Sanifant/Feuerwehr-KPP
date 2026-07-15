# Build stage
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Runtime stage
FROM nginx:alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/templates/default.conf.template

COPY docker-entrypoint.sh /docker-entrypoint.d/10-envsubst.sh
RUN chmod +x /docker-entrypoint.d/10-envsubst.sh

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
