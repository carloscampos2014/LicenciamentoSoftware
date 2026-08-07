"""LicenseManager SDK — Python client para a API de validação."""
from .client import LicenseManagerClient
from .models import LoginResult, InstallationResult
from .exceptions import LicenseManagerException

__all__ = ["LicenseManagerClient", "LoginResult", "InstallationResult", "LicenseManagerException"]
__version__ = "1.0.0"
