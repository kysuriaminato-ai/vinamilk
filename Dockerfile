# ====================================================================================================
# VINAMILK HRIS - MULTI-STAGE SECURE DOCKERFILE (PHP 8.2-FPM + NGINX ALPINE)
# ====================================================================================================
# Tuân thủ nguyên tắc Least Privilege, Non-root user, OPcache tối ưu hiệu năng
# ====================================================================================================

# STAGE 1: Composer Build & Dependencies
FROM composer:2.7 AS vendor
WORKDIR /app
COPY composer.json composer.lock* ./
RUN composer install \
    --no-dev \
    --no-interaction \
    --prefer-dist \
    --optimize-autoloader \
    --ignore-platform-reqs

# STAGE 2: Production Runtime
FROM php:8.2-fpm-alpine AS production

# Cài đặt các gói hệ thống cần thiết và extension PHP
RUN apk add --no-cache \
    nginx \
    supervisor \
    curl \
    libpng-dev \
    libjpeg-turbo-dev \
    freetype-dev \
    libzip-dev \
    icu-dev \
    oniguruma-dev \
    bash \
    tzdata \
    && cp /usr/share/zoneinfo/Asia/Ho_Chi_Minh /etc/localtime \
    && echo "Asia/Ho_Chi_Minh" > /etc/timezone \
    && docker-php-ext-configure gd --with-freetype --with-jpeg \
    && docker-php-ext-install -j$(nproc) \
        pdo_mysql \
        mbstring \
        bcmath \
        zip \
        gd \
        intl \
        opcache

# Cấu hình bảo mật PHP & OPcache
RUN { \
        echo 'opcache.memory_consumption=128'; \
        echo 'opcache.interned_strings_buffer=8'; \
        echo 'opcache.max_accelerated_files=10000'; \
        echo 'opcache.revalidate_freq=0'; \
        echo 'opcache.validate_timestamps=0'; \
        echo 'opcache.enable_cli=1'; \
    } > /usr/local/etc/php/conf.d/opcache-recommended.ini \
    && { \
        echo 'expose_php = Off'; \
        echo 'display_errors = Off'; \
        echo 'log_errors = On'; \
        echo 'error_log = /var/log/php_errors.log'; \
        echo 'memory_limit = 256M'; \
        echo 'upload_max_filesize = 10M'; \
        echo 'post_max_size = 12M'; \
        echo 'session.cookie_httponly = 1'; \
        echo 'session.cookie_secure = 1'; \
        echo 'session.cookie_samesite = "Lax"'; \
    } > /usr/local/etc/php/conf.d/security.ini

WORKDIR /var/www/html

# Sao chép mã nguồn ứng dụng
COPY . .
COPY --from=vendor /app/vendor ./vendor

# Phân quyền cho non-root user
RUN addgroup -g 1000 -S vinamilk && \
    adduser -u 1000 -S vinamilk -G vinamilk && \
    mkdir -p /var/www/html/storage /var/www/html/bootstrap/cache /var/log/supervisor && \
    chown -R vinamilk:vinamilk /var/www/html/storage /var/www/html/bootstrap/cache

# Cấu hình Nginx bảo mật
RUN { \
        echo 'server {'; \
        echo '    listen 8080;'; \
        echo '    server_name localhost;'; \
        echo '    root /var/www/html/public;'; \
        echo '    index index.php index.html;'; \
        echo '    server_tokens off;'; \
        echo '    add_header X-Frame-Options "DENY" always;'; \
        echo '    add_header X-Content-Type-Options "nosniff" always;'; \
        echo '    add_header X-XSS-Protection "1; mode=block" always;'; \
        echo '    add_header Content-Security-Policy "default-src '\''self'\''; script-src '\''self'\'' '\''unsafe-inline'\'' cdn.jsdelivr.net; style-src '\''self'\'' '\''unsafe-inline'\'' cdn.jsdelivr.net fonts.googleapis.com; font-src '\''self'\'' fonts.gstatic.com cdn.jsdelivr.net; img-src '\''self'\'' data: i.pravatar.cc;" always;'; \
        echo '    location / { try_files $uri $uri/ /index.php?$query_string; }'; \
        echo '    location ~ \.php$ {'; \
        echo '        fastcgi_pass 127.0.0.1:9000;'; \
        echo '        fastcgi_index index.php;'; \
        echo '        include fastcgi_params;'; \
        echo '        fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;'; \
        echo '    }'; \
        echo '    location ~ /\.(?!well-known).* { deny all; }'; \
        echo '}'; \
    } > /etc/nginx/http.d/default.conf

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["sh", "-c", "php-fpm -D && nginx -g 'daemon off;'"]
