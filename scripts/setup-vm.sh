#!/bin/bash
# =============================================================================
# setup-vm.sh — Configuração inicial da Oracle Cloud VM
# LicenciamentoSoftware — Fase 11
#
# Executa UMA VEZ na VM após o primeiro acesso.
# Instala: .NET 10, Nginx, configura systemd service e diretórios.
#
# Uso:
#   chmod +x setup-vm.sh
#   sudo ./setup-vm.sh
# =============================================================================

set -euo pipefail

APP_USER="www-data"
APP_DIR="/opt/licenciamento/api"
WEB_DIR="/var/www/licensemanager"
ENV_FILE="/etc/licenciamento/env"
SERVICE_NAME="licenciamento-api"

echo ""
echo "======================================================"
echo "  LicenciamentoSoftware — Setup da VM Oracle Cloud"
echo "======================================================"
echo ""

# ── 1. Atualizar sistema ──────────────────────────────────────────────────────
echo "[1/8] Atualizando pacotes..."
apt-get update -qq
apt-get upgrade -y -qq

# ── 2. Instalar .NET 10 ───────────────────────────────────────────────────────
echo "[2/8] Instalando .NET 10 Runtime..."
if ! dotnet --version 2>/dev/null | grep -q "^10\."; then
    # Adiciona o repositório Microsoft
    wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    rm /tmp/packages-microsoft-prod.deb
    apt-get update -qq
    apt-get install -y -qq aspnetcore-runtime-10.0
    echo "     .NET 10 instalado: $(dotnet --version)"
else
    echo "     .NET 10 já instalado: $(dotnet --version)"
fi

# ── 3. Instalar Nginx ─────────────────────────────────────────────────────────
echo "[3/8] Instalando Nginx..."
apt-get install -y -qq nginx
systemctl enable nginx
echo "     Nginx instalado"

# ── 4. Criar diretórios ───────────────────────────────────────────────────────
echo "[4/8] Criando estrutura de diretórios..."
mkdir -p "$APP_DIR"
mkdir -p "$WEB_DIR"
mkdir -p /etc/licenciamento
mkdir -p /var/log/licenciamento

chown -R "$APP_USER:$APP_USER" "$APP_DIR"
chown -R "$APP_USER:$APP_USER" "$WEB_DIR"
chown -R root:root /etc/licenciamento
chmod 750 /etc/licenciamento

echo "     Diretórios criados"

# ── 5. Criar arquivo de variáveis de ambiente ─────────────────────────────────
echo "[5/8] Criando arquivo de ambiente..."
if [ ! -f "$ENV_FILE" ]; then
    cat > "$ENV_FILE" << 'EOF'
# Variáveis de ambiente da API LicenciamentoSoftware
# Preencha os valores abaixo antes de iniciar o serviço
# Este arquivo é lido pelo systemd — não commitar no git

ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5016

# Banco de dados (PostgreSQL local na VM)
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=licenciamento;Username=postgres;Password=PREENCHER

# JWT
JwtSettings__Secret=PREENCHER
JwtSettings__Emissor=LicenciamentoSoftware
JwtSettings__Audiencia=LicenciamentoSoftware
JwtSettings__AccessTokenMinutos=60

# HMAC
LicencaTokenSettings__DefaultExpiracaoMinutos=525600
LicencaTokenSettings__AntiReplayJanelaMinutos=5

# CORS — domínio do frontend
Cors__AllowedOrigins__0=https://licensemanager.enzojb.com.br
EOF
    chmod 640 "$ENV_FILE"
    echo "     Arquivo criado em $ENV_FILE"
    echo "     ⚠️  PREENCHA os valores antes de iniciar o serviço!"
else
    echo "     Arquivo já existe em $ENV_FILE — não sobrescrito"
fi

# ── 6. Criar systemd service ──────────────────────────────────────────────────
echo "[6/8] Criando serviço systemd..."
cat > "/etc/systemd/system/${SERVICE_NAME}.service" << EOF
[Unit]
Description=LicenciamentoSoftware API
After=network.target
Wants=network-online.target

[Service]
Type=notify
User=$APP_USER
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/LicenciamentoSoftware.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$SERVICE_NAME
EnvironmentFile=$ENV_FILE
StandardOutput=journal
StandardError=journal

# Segurança
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}"
echo "     Serviço criado e habilitado (não iniciado — aguarda deploy)"

# ── 7. Configurar Nginx ───────────────────────────────────────────────────────
echo "[7/8] Configurando Nginx..."

# Site da API
cat > /etc/nginx/sites-available/licenciamento-api << 'EOF'
server {
    listen 80;
    server_name licensemanager-api.enzojb.com.br;

    # Cloudflare faz o TLS — Nginx só recebe HTTP interno
    # Para aceitar só tráfego do Cloudflare, configure o firewall

    location / {
        proxy_pass         http://127.0.0.1:5016;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
    }
}
EOF

# Site do Frontend (Blazor WASM estático)
cat > /etc/nginx/sites-available/licenciamento-web << 'EOF'
server {
    listen 80;
    server_name licensemanager.enzojb.com.br;

    root /var/www/licensemanager;
    index index.html;

    # Blazor WASM — todas as rotas redirecionam para index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache para assets estáticos (WASM, JS, CSS)
    location ~* \.(wasm|js|css|png|svg|ico|woff2?)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Compressão
    gzip on;
    gzip_types text/plain text/css application/javascript application/wasm;
}
EOF

# Ativar os sites
ln -sf /etc/nginx/sites-available/licenciamento-api /etc/nginx/sites-enabled/
ln -sf /etc/nginx/sites-available/licenciamento-web /etc/nginx/sites-enabled/

# Remover o site default do Nginx
rm -f /etc/nginx/sites-enabled/default

# Testar e recarregar
nginx -t && systemctl reload nginx
echo "     Nginx configurado"

# ── 8. Configurar firewall (ufw) ──────────────────────────────────────────────
echo "[8/8] Configurando firewall..."
ufw --force enable
ufw allow 22022/tcp comment "SSH"
ufw allow 80/tcp  comment "HTTP (Nginx/Cloudflare)"
ufw allow 443/tcp comment "HTTPS (Cloudflare)"
# NÃO expor porta 5016 diretamente — só via Nginx
ufw status numbered
echo "     Firewall configurado"

# ── Resumo ────────────────────────────────────────────────────────────────────
echo ""
echo "======================================================"
echo "  ✅ Setup concluído!"
echo "======================================================"
echo ""
echo "Próximos passos:"
echo ""
echo "  1. Preencha as variáveis em $ENV_FILE:"
echo "     sudo nano $ENV_FILE"
echo ""
echo "  2. Configure os registros DNS no Cloudflare:"
echo "     A  licensemanager.enzojb.com.br     → 137.131.209.235  (proxied)"
echo "     A  licensemanager-api.enzojb.com.br → 137.131.209.235  (proxied)"
echo ""
echo "  3. Faça push para master — o GitHub Actions fará o deploy automático"
echo ""
echo "  4. Após o deploy, inicie o serviço:"
echo "     sudo systemctl start $SERVICE_NAME"
echo "     sudo systemctl status $SERVICE_NAME"
echo ""
