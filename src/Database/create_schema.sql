CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Cliente" (
    "Id" uuid NOT NULL,
    "RazaoSocial" character varying(200) NOT NULL,
    "TipoInscricao" integer NOT NULL,
    "NumeroInscricao" character varying(20) NOT NULL,
    "Email" character varying(300) NOT NULL,
    "Telefone" character varying(15),
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_Cliente" PRIMARY KEY ("Id")
);

CREATE TABLE "TipoLicenca" (
    "Id" uuid NOT NULL,
    "Descricao" character varying(200) NOT NULL,
    CONSTRAINT "PK_TipoLicenca" PRIMARY KEY ("Id")
);

CREATE TABLE "ClienteFinal" (
    "Id" uuid NOT NULL,
    "IdCliente" uuid NOT NULL,
    "RazaoSocial" character varying(200) NOT NULL,
    "TipoInscricao" integer NOT NULL,
    "NumeroInscricao" character varying(20) NOT NULL,
    "Email" character varying(300) NOT NULL,
    "Telefone" character varying(15),
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_ClienteFinal" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ClienteFinal_Cliente_IdCliente" FOREIGN KEY ("IdCliente") REFERENCES "Cliente" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Usuario" (
    "Id" uuid NOT NULL,
    "IdCliente" uuid NOT NULL,
    "Nome" character varying(200) NOT NULL,
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_Usuario" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Usuario_Cliente_IdCliente" FOREIGN KEY ("IdCliente") REFERENCES "Cliente" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Aplicacao" (
    "Id" uuid NOT NULL,
    "IdCliente" uuid NOT NULL,
    "Titulo" character varying(120) NOT NULL,
    "Descricao" character varying(300),
    "IdTipoLicenca" uuid NOT NULL,
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_Aplicacao" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Aplicacao_Cliente_IdCliente" FOREIGN KEY ("IdCliente") REFERENCES "Cliente" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Aplicacao_TipoLicenca_IdTipoLicenca" FOREIGN KEY ("IdTipoLicenca") REFERENCES "TipoLicenca" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LogOperacao" (
    "Id" uuid NOT NULL,
    "Entidade" character varying(100) NOT NULL,
    "IdRegistro" uuid NOT NULL,
    "Operacao" character(1) NOT NULL,
    "DataHora" timestamp with time zone NOT NULL,
    "IdUsuario" uuid,
    "CamposAlterados" text,
    CONSTRAINT "PK_LogOperacao" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LogOperacao_Usuario_IdUsuario" FOREIGN KEY ("IdUsuario") REFERENCES "Usuario" ("Id")
);

CREATE TABLE "Licenca" (
    "Id" uuid NOT NULL,
    "IdCliente" uuid NOT NULL,
    "IdClienteFinal" uuid NOT NULL,
    "IdAplicativo" uuid NOT NULL,
    "DataCadastro" timestamp with time zone NOT NULL,
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_Licenca" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Licenca_Aplicacao_IdAplicativo" FOREIGN KEY ("IdAplicativo") REFERENCES "Aplicacao" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Licenca_ClienteFinal_IdClienteFinal" FOREIGN KEY ("IdClienteFinal") REFERENCES "ClienteFinal" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Licenca_Cliente_IdCliente" FOREIGN KEY ("IdCliente") REFERENCES "Cliente" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LicencaInstalacao" (
    "Id" uuid NOT NULL,
    "LicencaId" uuid NOT NULL,
    "QuantidadeMaxima" integer NOT NULL,
    CONSTRAINT "PK_LicencaInstalacao" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LicencaInstalacao_Licenca_LicencaId" FOREIGN KEY ("LicencaId") REFERENCES "Licenca" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LicencaInstalacaoRegistrada" (
    "Id" uuid NOT NULL,
    "LicencaId" uuid NOT NULL,
    "IdentificadorMaquina" character varying(300) NOT NULL,
    "DataRegistro" timestamp with time zone NOT NULL,
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_LicencaInstalacaoRegistrada" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LicencaInstalacaoRegistrada_Licenca_LicencaId" FOREIGN KEY ("LicencaId") REFERENCES "Licenca" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LicencaPeriodo" (
    "Id" uuid NOT NULL,
    "LicencaId" uuid NOT NULL,
    "DataInicio" timestamp with time zone NOT NULL,
    "DataFim" timestamp with time zone NOT NULL,
    "RenovacaoAutomatica" boolean NOT NULL,
    CONSTRAINT "PK_LicencaPeriodo" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LicencaPeriodo_Licenca_LicencaId" FOREIGN KEY ("LicencaId") REFERENCES "Licenca" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LicencaSessao" (
    "Id" uuid NOT NULL,
    "LicencaId" uuid NOT NULL,
    "IdentificadorUsuario" character varying(300) NOT NULL,
    "DataLogin" timestamp with time zone NOT NULL,
    "DataUltimaAtividade" timestamp with time zone NOT NULL,
    "Ativo" boolean NOT NULL,
    CONSTRAINT "PK_LicencaSessao" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LicencaSessao_Licenca_LicencaId" FOREIGN KEY ("LicencaId") REFERENCES "Licenca" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LicencaUsuarios" (
    "Id" uuid NOT NULL,
    "LicencaId" uuid NOT NULL,
    "QuantidadeMaxima" integer NOT NULL,
    "MaxSessoesPorUsuario" integer NOT NULL,
    "TempoLimiteSessaoHoras" integer NOT NULL,
    CONSTRAINT "PK_LicencaUsuarios" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LicencaUsuarios_Licenca_LicencaId" FOREIGN KEY ("LicencaId") REFERENCES "Licenca" ("Id") ON DELETE CASCADE
);

INSERT INTO "TipoLicenca" ("Id", "Descricao")
VALUES ('11111111-1111-1111-1111-111111111111', 'Permanente');
INSERT INTO "TipoLicenca" ("Id", "Descricao")
VALUES ('22222222-2222-2222-2222-222222222222', 'Por Período');
INSERT INTO "TipoLicenca" ("Id", "Descricao")
VALUES ('33333333-3333-3333-3333-333333333333', 'Por Usuários');
INSERT INTO "TipoLicenca" ("Id", "Descricao")
VALUES ('44444444-4444-4444-4444-444444444444', 'Por Instalação');

CREATE INDEX "IX_Aplicacao_IdCliente" ON "Aplicacao" ("IdCliente");

CREATE INDEX "IX_Aplicacao_IdTipoLicenca" ON "Aplicacao" ("IdTipoLicenca");

CREATE INDEX "IX_ClienteFinal_IdCliente" ON "ClienteFinal" ("IdCliente");

CREATE INDEX "IX_Licenca_IdAplicativo" ON "Licenca" ("IdAplicativo");

CREATE INDEX "IX_Licenca_IdClienteFinal" ON "Licenca" ("IdClienteFinal");

CREATE UNIQUE INDEX uq_licenca_combinacao_ativa ON "Licenca" ("IdCliente", "IdClienteFinal", "IdAplicativo") WHERE "Ativo" = true;

CREATE UNIQUE INDEX "IX_LicencaInstalacao_LicencaId" ON "LicencaInstalacao" ("LicencaId");

CREATE UNIQUE INDEX uq_licencainstalacao_maquina_ativa ON "LicencaInstalacaoRegistrada" ("LicencaId", "IdentificadorMaquina") WHERE "Ativo" = true;

CREATE UNIQUE INDEX "IX_LicencaPeriodo_LicencaId" ON "LicencaPeriodo" ("LicencaId");

CREATE INDEX "IX_LicencaSessao_LicencaId_Ativo" ON "LicencaSessao" ("LicencaId", "Ativo");

CREATE INDEX "IX_LicencaSessao_LicencaId_IdentificadorUsuario_Ativo" ON "LicencaSessao" ("LicencaId", "IdentificadorUsuario", "Ativo");

CREATE UNIQUE INDEX "IX_LicencaUsuarios_LicencaId" ON "LicencaUsuarios" ("LicencaId");

CREATE INDEX "IX_LogOperacao_Entidade_IdRegistro" ON "LogOperacao" ("Entidade", "IdRegistro");

CREATE INDEX "IX_LogOperacao_IdUsuario" ON "LogOperacao" ("IdUsuario");

CREATE INDEX "IX_Usuario_IdCliente" ON "Usuario" ("IdCliente");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260727183033_InitialCreate', '8.0.10');

COMMIT;

