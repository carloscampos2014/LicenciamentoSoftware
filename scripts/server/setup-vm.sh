#!/bin/bash
# =============================================================================
# setup-vm.sh — Configura a Oracle Cloud VM para hospedar a API e o Web
#
# Uso: bash setup-vm.sh
#
# O que faz:
#   1. Atualiza o sistema
#   2. Instala .NET 10 runtime
#   3. Instala Nginx
#   4. Cria estrutura de diretórios
#   5. Configura o service systemd da API
#   6. Configura Nginx (proxy API + estático Web)
#   7. Abre as portas no firewall do Ubuntu (ufw)
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
WEB_DIR="/var/www/licensemanager"
SERVICE_USER="licenciamento"
API_URL="https://licensemanager-api.enzojb.com.br"
WEB_DOMAIN="licensemanager.enzojb.com.br"
API_DOMAIN="licensemanager-api.enzojb.com.br"
API_PORT=5016

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
chown -R www-data:www-data "$WEB_DIR"
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
    # Tamanho máximo de upload (para payloads da API)
    client_max_body_size 10M;

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

# Config do Web (Blazor WASM)
cat > /etc/nginx/sites-available/web-licensemanager <<NGINXEOF
server {
    listen 80;
    server_name $WEB_DOMAIN;

    root $WEB_DIR;
    index index.html;

    # Necessário para Blazor WASM — todas as rotas servem index.html
    location / {
        try_files \$uri \$uri/ /index.html;
    }

    # Cache agressivo para assets imutáveis do Blazor
    location ~* \.(js|wasm|json|dll|dat|blat|pdb)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        add_header X-Content-Type-Options nosniff;
    }

    # Headers de segurança
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header Referrer-Policy strict-origin-when-cross-origin;
}
NGINXEOF

# Ativa os sites
ln -sf /etc/nginx/sites-available/api-licensemanager /etc/nginx/sites-enabled/
ln -sf /etc/nginx/sites-available/web-licensemanager /etc/nginx/sites-enabled/

# Testa configuração
nginx -t && systemctl reload nginx
ok "Nginx configurado para $WEB_DOMAIN e $API_DOMAIN"

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
echo "  2. Após o primeiro deploy da API via CI/CD, inicie o service:"
echo "     sudo systemctl enable --now licenciamento-api"
echo ""
echo "  3. Configure os registros DNS no Cloudflare:"
echo "     A  licensemanager.enzojb.com.br    → 137.131.209.235"
echo "     A  licensemanager-api.enzojb.com.br → 137.131.209.235"
echo ""
echo "  4. No Cloudflare, marque os registros como 'Proxied' (nuvem laranja)"
echo "     para ativar SSL e CDN automáticos."
echo ""
