#!/usr/bin/env bash
# =============================================================================
# setup-admin.sh — Instala o serviço LicenciamentoSoftware.Admin na Oracle VM
#
# O que faz:
#   1. Cria o arquivo de variáveis /etc/licenciamento/admin.env
#   2. Cria o systemd unit licenciamento-admin.service
#   3. Habilita e inicia o serviço
#   4. Verifica que a porta 5020 NÃO está exposta no ufw nem no Nginx
#
# Uso:
#   sudo bash scripts/server/setup-admin.sh
#
# IMPORTANTE: defina Admin__Senha antes de executar:
#   export ADMIN_SENHA="sua-senha-segura"
#   sudo -E bash scripts/server/setup-admin.sh
# =============================================================================

set -euo pipefail

ADMIN_DIR="/opt/licenciamento/admin"
ENV_FILE="/etc/licenciamento/admin.env"
SERVICE_NAME="licenciamento-admin"
ADMIN_USUARIO="${ADMIN_USUARIO:-admin}"
ADMIN_SENHA="${ADMIN_SENHA:-}"

if [ -z "$ADMIN_SENHA" ]; then
    echo "ERRO: defina a variável ADMIN_SENHA antes de executar."
    echo "  export ADMIN_SENHA='sua-senha-segura'"
    echo "  sudo -E bash $0"
    exit 1
fi

echo "==> Configurando LicenciamentoSoftware.Admin..."

# ── 1. Criar diretório de binários ────────────────────────────────────────────
mkdir -p "$ADMIN_DIR"
chown -R ubuntu:ubuntu "$ADMIN_DIR"

# ── 2. Criar arquivo de variáveis de ambiente ─────────────────────────────────
mkdir -p /etc/licenciamento

# Lê a connection string da env da API para reutilizar
API_CS=$(grep "^ConnectionStrings__DefaultConnection=" /etc/licenciamento/env 2>/dev/null \
    | cut -d'=' -f2- || echo "")

cat > "$ENV_FILE" << EOF
# Admin — variáveis de ambiente
# Gerado por setup-admin.sh — edite com cuidado

ConnectionStrings__DefaultConnection=${API_CS}
Admin__Usuario=${ADMIN_USUARIO}
Admin__Senha=${ADMIN_SENHA}
AdminSettings__ApiHealthUrl=http://localhost:5016/health
AdminSettings__BffHealthUrl=http://localhost:5017/health
AdminSettings__BackupScript=/opt/scripts/backup-db.sh
AdminSettings__BackupDir=/opt/backups
ASPNETCORE_ENVIRONMENT=Production
EOF

chmod 600 "$ENV_FILE"
chown root:root "$ENV_FILE"
echo "==> $ENV_FILE criado (chmod 600)"

# ── 3. Criar systemd unit ─────────────────────────────────────────────────────
cat > "/etc/systemd/system/${SERVICE_NAME}.service" << EOF
[Unit]
Description=LicenciamentoSoftware Admin Panel
After=network.target licenciamento-api.service

[Service]
Type=simple
User=ubuntu
WorkingDirectory=${ADMIN_DIR}
EnvironmentFile=${ENV_FILE}
ExecStart=/usr/bin/dotnet ${ADMIN_DIR}/LicenciamentoSoftware.Admin.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=licenciamento-admin

# Porta 5020 bind apenas em localhost — NUNCA exposta publicamente
Environment=ASPNETCORE_URLS=http://localhost:5020

[Install]
WantedBy=multi-user.target
EOF

echo "==> Service ${SERVICE_NAME}.service criado"

# ── 4. Verificar que o Nginx NÃO expõe a porta 5020 ──────────────────────────
if grep -r "5020" /etc/nginx/sites-enabled/ 2>/dev/null | grep -v "#"; then
    echo "AVISO: Nginx pode estar expondo a porta 5020. Verifique /etc/nginx/sites-enabled/"
else
    echo "==> Nginx: porta 5020 não exposta ✅"
fi

# ── 5. Verificar ufw — porta 5020 não deve ter regra ALLOW ───────────────────
if ufw status 2>/dev/null | grep -q "5020.*ALLOW"; then
    echo "AVISO: ufw tem regra ALLOW para porta 5020. Removendo..."
    ufw delete allow 5020/tcp 2>/dev/null || true
    echo "==> Regra removida"
else
    echo "==> ufw: porta 5020 não exposta ✅"
fi

# ── 6. Habilitar e iniciar o serviço ──────────────────────────────────────────
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl restart "$SERVICE_NAME"

echo ""
echo "==> LicenciamentoSoftware.Admin configurado!"
echo "    Serviço:  ${SERVICE_NAME}.service"
echo "    Porta:    5020 (localhost only)"
echo "    Acesso:   ssh -L 16000:localhost:5020 ... → http://localhost:16000"
echo "    Status:   sudo systemctl status ${SERVICE_NAME}"
