# Build stage - compile Angular application
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Production stage - serve with Nginx
FROM nginx:alpine
# Clear default nginx content
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build /app/dist/k8s-app-frontend-and-backend.client /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
