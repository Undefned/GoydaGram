# Используем актуальную версию, которая у вас на машине
FROM golang:1.26-alpine AS builder

RUN apk add --no-cache git ca-certificates

ENV GO111MODULE=on \
    CGO_ENABLED=0 \
    GOOS=linux \
    GOARCH=amd64 \
    GOPROXY=https://proxy.golang.org,direct

WORKDIR /app

COPY go.mod go.sum ./

RUN go mod download

COPY . .

RUN go build -ldflags="-w -s" -o /feed-service .

FROM alpine:latest

RUN apk --no-cache add ca-certificates && \
    update-ca-certificates

WORKDIR /root/

COPY --from=builder /feed-service .
COPY --from=builder /app/docs ./docs

EXPOSE 8080

CMD ["./feed-service"]