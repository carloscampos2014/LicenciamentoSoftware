#!/bin/bash
# =============================================================================
# setup-vm.sh — Configura a Oracle Cloud VM para hospedar a API e o BFF Web
#
# Uso: bash setup-vm.sh
#
# O que faz:
#   1. Atualiza o sistema
#   2. Instala .NET 10 runtime
#   3. Instala Nginx
#   4. Cria estrutura de diretórios
#   5. Configura o service systemd da API (porta 5016)
#   6. Configura o service systemd do BFF Web.Server (porta 5017)
#   7. Configura Nginx (proxy API + proxy BFF)
#   8. Abre as portas no firewall do Ubuntu (ufw)
# =============================================================================

set -e

# ── Cores ─────────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

step()  { echo -e "\n${CYAN}[>>] $1${NC}"; }
ok()    { echo -e "${GREEN}[OK] $1${NC}"; }
fail()  { echo -e "${RED}[!!] $1${NC}"; exit 1; }

# ── Variáveis ─────────────────────────────────────────────────────────────────
API_DIR="/opt/licenciamento/api"
WEB_DIR="/opt/licenciamento/web"
SERVICE_USER="licenciamento"
API_URL="https://licensemanager-api.enzojb.com.br"
WEB_DOMAIN="licensemanager.enzojb.com.br"
API_DOMAIN="licensemanager-api.enzojb.com.br"
API_PORT=5016
WEB_PORT=5017

# ── 1. Atualiza sistema ───────────────────────────────────────────────────────
step "Atualizando sistema..."
apt-get update -qq && apt-get upgrade -y -qq
ok "Sistema atualizado."

# ── 2. Instala .NET 10 ────────────────────────────────────────────────────────
step "Instalando .NET 10 runtime..."
if ! command -v dotnet &>/dev/null; then
    # Adiciona repositório Microsoft
    wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    rm /tmp/packages-microsoft-prod.deb
    apt-get update -qq
fi
apt-get install -y aspnetcore-runtime-10.0 2>/dev/null || \
apt-get install -y dotnet-runtime-10.0 aspnetcore-runtime-10.0
ok ".NET 10 instalado: $(dotnet --version)"

# ── 3. Instala Nginx ──────────────────────────────────────────────────────────
step "Instalando Nginx..."
apt-get install -y nginx
systemctl enable nginx
ok "Nginx instalado."

# ── 4. Cria usuário e diretórios ──────────────────────────────────────────────
step "Criando usuário e diretórios..."
if ! id "$SERVICE_USER" &>/dev/null; then
    useradd --system --no-create-home --shell /bin/false "$SERVICE_USER"
fi

mkdir -p "$API_DIR"
mkdir -p "$WEB_DIR"
chown -R "$SERVICE_USER:$SERVICE_USER" "$API_DIR"
chown -R "$SERVICE_USER:$SERVICE_USER" "$WEB_DIR"
ok "Diretórios criados: $API_DIR e $WEB_DIR"

# ── 5. Cria arquivo de variáveis de ambiente ──────────────────────────────────
step "Criando arquivo de variáveis de ambiente..."
if [ ! -f /etc/licenciamento/env ]; then
    mkdir -p /etc/licenciamento
    cat > /etc/licenciamento/env <<'ENVEOF'
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://localhost:5016
ConnectionStrings__DefaultConnection=PLACEHOLDER_CONNECTION_STRING
JwtSettings__Secret=PLACEHOLDER_JWT_SECRET
JwtSettings__Emissor=LicenciamentoSoftware
JwtSettings__Audiencia=LicenciamentoSoftware
JwtSettings__AccessTokenMinutos=60
ENVEOF
    chmod 600 /etc/licenciamento/env
    chown root:root /etc/licenciamento/env
    echo ""
    echo "  ⚠️  Edite /etc/licenciamento/env com os valores reais antes de iniciar a API"
    echo "  Execute: sudo nano /etc/licenciamento/env"
fi
ok "Arquivo de env criado em /etc/licenciamento/env"

# ── 6. Cria service systemd ───────────────────────────────────────────────────
step "Criando service systemd..."
cat > /etc/systemd/system/licenciamento-api.service <<SERVICEEOF
[Unit]
Description=LicenciamentoSoftware API
After=network.target
Wants=network.target

[Service]
Type=notify
User=$SERVICE_USER
WorkingDirectory=$API_DIR
ExecStart=/usr/bin/dotnet $API_DIR/LicenciamentoSoftware.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=licenciamento-api
EnvironmentFile=/etc/licenciamento/env

# Hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full

[Install]
WantedBy=multi-user.target
SERVICEEOF

systemctl daemon-reload
ok "Service licenciamento-api.service criado."

# ── 6b. Cria service systemd do BFF ──────────────────────────────────────────
step "Criando service systemd do BFF (Web.Server)..."

# Arquivo de env do BFF
if [ ! -f /etc/licenciamento/web.env ]; then
    cat > /etc/licenciamento/web.env <<'ENVEOF'
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://localhost:5017
ENVEOF
    chmod 640 /etc/licenciamento/web.env
    chown root:root /etc/licenciamento/web.env
fi

cat > /etc/systemd/system/licenciamento-web.service <<SERVICEEOF
[Unit]
Description=LicenciamentoSoftware Web BFF
After=network.target licenciamento-api.service
Wants=network.target

[Service]
Type=simple

[Install]
WantedBy=multi-user.target
SERVICEEOF

systemctl daemon-reload
ok "Service licenciamento-web.service criado."

# ── 7. Configura Nginx ────────────────────────────────────────────────────────
step "Configurando Nginx..."

# Remove config padrão
rm -f /etc/nginx/sites-enabled/default

# Config da API
cat > /etc/nginx/sites-available/api-licensemanager <<NGINXEOF
server {
    listen 80;
    server_name $API_DOMAIN;

    # Cloudflare faz o TLS — aqui só HTTP
    client_max_body_size 10M;

    # ── Cabeçalhos de segurança HTTP ──────────────────────────────────────────
    add_header X-Frame-Options           "SAMEORIGIN"                         always;
    add_header X-Content-Type-Options    "nosniff"                            always;
    add_header Referrer-Policy           "strict-origin-when-cross-origin"    always;
    add_header Permissions-Policy        "camera=(), microphone=(), geolocation=()" always;
    add_header Content-Security-Policy   "default-src 'self'; frame-ancestors 'none';" always;

    location / {
        proxy_pass         http://localhost:$API_PORT;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 90s;
    }
}
NGINXEOF

# Config do BFF Web (Blazor WASM + BFF)
cat > /etc/nginx/sites-available/web-licensemanager <<NGINXEOF
server {
    listen 80;
    server_name $WEB_DOMAIN;

    # Cloudflare faz o TLS — Nginx só recebe HTTP
    # O BFF (ASP.NET Core) serve o WASM estático e processa /bff/*

    # ── Cabeçalhos de segurança HTTP ──────────────────────────────────────────
    # CSP permissivo para Blazor WASM: carrega scripts, estilos inline e
    # conecta com a API downstream
    add_header X-Frame-Options           "SAMEORIGIN"                         always;
    add_header X-Content-Type-Options    "nosniff"                            always;
    add_header Referrer-Policy           "strict-origin-when-cross-origin"    always;
    add_header Permissions-Policy        "camera=(), microphone=(), geolocation=()" always;
    add_header Content-Security-Policy   "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' https://$API_DOMAIN;" always;

    location / {
        proxy_pass         http://localhost:$WEB_PORT;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
        proxy_buffer_size   128k;
        proxy_buffers       4 256k;
        proxy_busy_buffers_size 256k;
    }
}
NGINXEOF

# Ativa os sites
ln -sf /etc/nginx/sites-available/api-licensemanager /etc/nginx/sites-enabled/
ln -sf /etc/nginx/sites-available/web-licensemanager /etc/nginx/sites-enabled/

# Testa configuração
nginx -t && systemctl reload nginx
ok "Nginx configurado para $WEB_DOMAIN (BFF porta $WEB_PORT) e $API_DOMAIN (API porta $API_PORT)"

# ── 8. Configura firewall ─────────────────────────────────────────────────────
step "Configurando firewall (ufw)..."
ufw --force enable
ufw allow 22022/tcp  comment "SSH"
ufw allow 80/tcp     comment "HTTP (Nginx)"
ufw allow 443/tcp    comment "HTTPS (Cloudflare)"
ok "Firewall configurado."

# ── Resumo ────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Setup concluído!${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════════${NC}"
echo ""
echo "  Próximos passos:"
echo "  1. Edite as variáveis de ambiente:"
echo "     sudo nano /etc/licenciamento/env"
echo ""
echo "  2. Após o primeiro deploy via CI/CD, inicie os services:"
echo "     sudo systemctl enable --now licenciamento-api"
echo "     sudo systemctl enable --now licenciamento-web"
echo ""
echo "  3. Configure os registros DNS no Cloudflare:"
echo "     A  licensemanager.enzojb.com.br    → 137.131.209.235"
echo "     A  licensemanager-api.enzojb.com.br → 137.131.209.235"
echo ""
echo "  4. No Cloudflare, marque os registros como 'Proxied' (nuvem laranja)"
echo "     para ativar SSL e CDN automáticos."
echo ""
