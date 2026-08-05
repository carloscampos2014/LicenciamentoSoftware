#!/usr/bin/env bash
# =============================================================================
# setup-backup.sh — Configura backup automático do PostgreSQL na Oracle VM
#
# O que faz:
#   1. Cria o script /opt/scripts/backup-db.sh (pg_dump + gzip)
#   2. Instala cron job diário às 2h UTC
#   3. Cria diretório /opt/backups com permissões corretas
#
# Uso:
#   sudo bash scripts/server/setup-backup.sh
#
# Após executar, o backup será gerado diariamente em:
#   /opt/backups/licenciamento_YYYYMMDD_HHMMSS.sql.gz
#
# Retenção: 7 dias (arquivos mais antigos são excluídos automaticamente)
# =============================================================================

set -euo pipefail

BACKUP_DIR="/opt/backups"
SCRIPT_DIR="/opt/scripts"
BACKUP_SCRIPT="$SCRIPT_DIR/backup-db.sh"
DB_NAME="licenciamento"
DB_USER="licenciamento"
RETENTION_DAYS=7
LOG_FILE="/var/log/licenciamento/backup.log"

echo "==> Configurando backup automático do PostgreSQL..."

# ── 1. Criar diretórios ───────────────────────────────────────────────────────
mkdir -p "$BACKUP_DIR" "$SCRIPT_DIR"
chown -R ubuntu:ubuntu "$BACKUP_DIR" "$SCRIPT_DIR"
chmod 750 "$BACKUP_DIR" "$SCRIPT_DIR"

# ── 2. Criar script de backup ─────────────────────────────────────────────────
cat > "$BACKUP_SCRIPT" << 'BACKUP_EOF'
#!/usr/bin/env bash
# backup-db.sh — Executa pg_dump e remove backups antigos
# Gerado por setup-backup.sh — não editar manualmente.
set -euo pipefail

BACKUP_DIR="/opt/backups"
DB_NAME="licenciamento"
DB_USER="licenciamento"
RETENTION_DAYS=7
LOG_FILE="/var/log/licenciamento/backup.log"
TIMESTAMP=$(date -u +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/licenciamento_${TIMESTAMP}.sql.gz"

log() { echo "[$(date -u '+%Y-%m-%d %H:%M:%S UTC')] $*" | tee -a "$LOG_FILE"; }

log "Iniciando backup: $BACKUP_FILE"

# pg_dump comprimido via gzip
PGPASSWORD="" pg_dump \
    --host=localhost \
    --port=5432 \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --no-password \
    --format=plain \
    --blobs \
    | gzip -9 > "$BACKUP_FILE"

TAMANHO=$(du -sh "$BACKUP_FILE" | cut -f1)
log "Backup concluído: $BACKUP_FILE ($TAMANHO)"

# Remove backups mais antigos que RETENTION_DAYS dias
REMOVIDOS=$(find "$BACKUP_DIR" -name "licenciamento_*.sql.gz" \
    -mtime +${RETENTION_DAYS} -delete -print | wc -l)
[ "$REMOVIDOS" -gt 0 ] && log "Backups antigos removidos: $REMOVIDOS arquivo(s)"

log "Concluído com sucesso."
BACKUP_EOF

chmod +x "$BACKUP_SCRIPT"
chown ubuntu:ubuntu "$BACKUP_SCRIPT"

# ── 3. Garantir que o arquivo de log existe ───────────────────────────────────
mkdir -p "$(dirname "$LOG_FILE")"
touch "$LOG_FILE"
chown ubuntu:ubuntu "$LOG_FILE"

# ── 4. Instalar cron job (ubuntu) — diário às 2h UTC ─────────────────────────
CRON_JOB="0 2 * * * $BACKUP_SCRIPT >> $LOG_FILE 2>&1"

# Remove entradas anteriores do mesmo script (idempotente)
crontab -u ubuntu -l 2>/dev/null | grep -v "$BACKUP_SCRIPT" > /tmp/crontab_tmp || true
echo "$CRON_JOB" >> /tmp/crontab_tmp
crontab -u ubuntu /tmp/crontab_tmp
rm -f /tmp/crontab_tmp

echo "==> Cron job instalado: $CRON_JOB"

# ── 5. Executar primeiro backup imediatamente ─────────────────────────────────
echo "==> Executando primeiro backup..."
sudo -u ubuntu bash "$BACKUP_SCRIPT"

echo ""
echo "==> Configuração de backup concluída!"
echo "    Backups em:  $BACKUP_DIR"
echo "    Script:      $BACKUP_SCRIPT"
echo "    Log:         $LOG_FILE"
echo "    Retenção:    $RETENTION_DAYS dias"
echo "    Agendamento: diário às 2h UTC"
