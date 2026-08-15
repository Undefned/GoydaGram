FROM golang:1.25-alpine AS builder

# Устанавливаем git и ca-certificates
RUN apk add --no-cache git ca-certificates

# Настройки для скачивания модулей
ENV GO111MODULE=on \
    CGO_ENABLED=0 \
    GOOS=linux \
    GOARCH=amd64 \
    GOPROXY=https://proxy.golang.org,direct

WORKDIR /app

# Копируем файлы модулей
COPY go.mod go.sum ./

# Скачиваем зависимости
RUN go mod download

# Копируем остальной код
COPY . .

# Генерируем Swagger документацию
RUN go install github.com/swaggo/swag/cmd/swag@latest && \
    swag init --parseDependency --parseInternal || true

# Собираем (без -mod=vendor)
RUN go build -ldflags="-w -s" -o /social-service .

FROM alpine:latest

RUN apk --no-cache add ca-certificates && \
    update-ca-certificates

WORKDIR /root/

COPY --from=builder /social-service .
COPY --from=builder /app/docs ./docs

EXPOSE 8080

CMD ["./social-service"]