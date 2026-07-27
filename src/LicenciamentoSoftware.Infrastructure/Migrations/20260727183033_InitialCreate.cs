using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LicenciamentoSoftware.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoInscricao = table.Column<int>(type: "integer", nullable: false),
                    NumeroInscricao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoLicenca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoLicenca", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClienteFinal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoInscricao = table.Column<int>(type: "integer", nullable: false),
                    NumeroInscricao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteFinal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteFinal_Cliente_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Cliente_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aplicacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IdTipoLicenca = table.Column<Guid>(type: "uuid", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aplicacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aplicacao_Cliente_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Aplicacao_TipoLicenca_IdTipoLicenca",
                        column: x => x.IdTipoLicenca,
                        principalTable: "TipoLicenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogOperacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Entidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdRegistro = table.Column<Guid>(type: "uuid", nullable: false),
                    Operacao = table.Column<char>(type: "character(1)", nullable: false),
                    DataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: true),
                    CamposAlterados = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogOperacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogOperacao_Usuario_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Licenca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    IdClienteFinal = table.Column<Guid>(type: "uuid", nullable: false),
                    IdAplicativo = table.Column<Guid>(type: "uuid", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Licenca_Aplicacao_IdAplicativo",
                        column: x => x.IdAplicativo,
                        principalTable: "Aplicacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licenca_ClienteFinal_IdClienteFinal",
                        column: x => x.IdClienteFinal,
                        principalTable: "ClienteFinal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licenca_Cliente_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicencaInstalacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicencaId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantidadeMaxima = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicencaInstalacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicencaInstalacao_Licenca_LicencaId",
                        column: x => x.LicencaId,
                        principalTable: "Licenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicencaInstalacaoRegistrada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicencaId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentificadorMaquina = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicencaInstalacaoRegistrada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicencaInstalacaoRegistrada_Licenca_LicencaId",
                        column: x => x.LicencaId,
                        principalTable: "Licenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicencaPeriodo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicencaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RenovacaoAutomatica = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicencaPeriodo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicencaPeriodo_Licenca_LicencaId",
                        column: x => x.LicencaId,
                        principalTable: "Licenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicencaSessao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicencaId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentificadorUsuario = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DataLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataUltimaAtividade = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicencaSessao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicencaSessao_Licenca_LicencaId",
                        column: x => x.LicencaId,
                        principalTable: "Licenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicencaUsuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicencaId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantidadeMaxima = table.Column<int>(type: "integer", nullable: false),
                    MaxSessoesPorUsuario = table.Column<int>(type: "integer", nullable: false),
                    TempoLimiteSessaoHoras = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicencaUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicencaUsuarios_Licenca_LicencaId",
                        column: x => x.LicencaId,
                        principalTable: "Licenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TipoLicenca",
                columns: new[] { "Id", "Descricao" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Permanente" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Por Período" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Por Usuários" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Por Instalação" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aplicacao_IdCliente",
                table: "Aplicacao",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Aplicacao_IdTipoLicenca",
                table: "Aplicacao",
                column: "IdTipoLicenca");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteFinal_IdCliente",
                table: "ClienteFinal",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Licenca_IdAplicativo",
                table: "Licenca",
                column: "IdAplicativo");

            migrationBuilder.CreateIndex(
                name: "IX_Licenca_IdClienteFinal",
                table: "Licenca",
                column: "IdClienteFinal");

            migrationBuilder.CreateIndex(
                name: "uq_licenca_combinacao_ativa",
                table: "Licenca",
                columns: new[] { "IdCliente", "IdClienteFinal", "IdAplicativo" },
                unique: true,
                filter: "\"Ativo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_LicencaInstalacao_LicencaId",
                table: "LicencaInstalacao",
                column: "LicencaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_licencainstalacao_maquina_ativa",
                table: "LicencaInstalacaoRegistrada",
                columns: new[] { "LicencaId", "IdentificadorMaquina" },
                unique: true,
                filter: "\"Ativo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_LicencaPeriodo_LicencaId",
                table: "LicencaPeriodo",
                column: "LicencaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LicencaSessao_LicencaId_Ativo",
                table: "LicencaSessao",
                columns: new[] { "LicencaId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_LicencaSessao_LicencaId_IdentificadorUsuario_Ativo",
                table: "LicencaSessao",
                columns: new[] { "LicencaId", "IdentificadorUsuario", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_LicencaUsuarios_LicencaId",
                table: "LicencaUsuarios",
                column: "LicencaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogOperacao_Entidade_IdRegistro",
                table: "LogOperacao",
                columns: new[] { "Entidade", "IdRegistro" });

            migrationBuilder.CreateIndex(
                name: "IX_LogOperacao_IdUsuario",
                table: "LogOperacao",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_IdCliente",
                table: "Usuario",
                column: "IdCliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicencaInstalacao");

            migrationBuilder.DropTable(
                name: "LicencaInstalacaoRegistrada");

            migrationBuilder.DropTable(
                name: "LicencaPeriodo");

            migrationBuilder.DropTable(
                name: "LicencaSessao");

            migrationBuilder.DropTable(
                name: "LicencaUsuarios");

            migrationBuilder.DropTable(
                name: "LogOperacao");

            migrationBuilder.DropTable(
                name: "Licenca");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Aplicacao");

            migrationBuilder.DropTable(
                name: "ClienteFinal");

            migrationBuilder.DropTable(
                name: "TipoLicenca");

            migrationBuilder.DropTable(
                name: "Cliente");
        }
    }
}
