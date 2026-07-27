<#
.SYNOPSIS
Script para iniciar ambiente local: cria container PostgreSQL via Docker (ou reutiliza), aplica migrations EF Core e inicia a API.

USAGE (PowerShell):
  .\scripts\start-local.ps1            # cria container (se necessário), aplica migrations e executa API
  .\scripts\start-local.ps1 -RecreateContainer  # força recriar o container PostgreSQL
#>

	[switch]$RecreateContainer
)

	return (Get-Command $name -ErrorAction SilentlyContinue) -ne $null
}

	Write-Error "Docker não encontrado. Instale o Docker Desktop ou use WSL2 e habilite o Docker.";
	exit 1
}

	Write-Error "dotnet (SDK) não encontrado. Instale o .NET 8 SDK.";
	exit 1
}



	if ($RecreateContainer) {
		Write-Host "Stopping and removing existing container $containerName..."
		& docker stop $containerName | Out-Null
		& docker rm $containerName | Out-Null
	} else {
		$running = (& docker ps --filter "name=$containerName" --format "{{.Names}}") -join "`n"
		if ($running -match $containerName) {
			Write-Host "Container $containerName is already running."
		} else {
			Write-Host "Starting existing container $containerName..."
			& docker start $containerName | Out-Null
		}
	}
}

	Write-Host "Creating and starting container $containerName..."
	& docker run --name $containerName -e POSTGRES_USER=$dbUser -e POSTGRES_PASSWORD=$dbPass -e POSTGRES_DB=$dbName -p $hostPort:5432 -d $image | Out-Null
}

	& docker exec $containerName pg_isready -U $dbUser > $null 2>&1
	if ($LASTEXITCODE -eq 0) { break }
	Start-Sleep -Seconds 2
	$attempt++
}
if ($attempt -ge $maxAttempts) {
	Write-Error "PostgreSQL não ficou pronto no tempo esperado.";
	exit 1
}

	Write-Error "Falha ao aplicar migrations. Saindo.";
	exit $LASTEXITCODE
}

