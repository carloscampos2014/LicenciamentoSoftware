from dataclasses import dataclass
from typing import Optional


@dataclass
class LoginResult:
    authorized: bool
    session_id: Optional[str]


@dataclass
class InstallationResult:
    authorized: bool
    installation_id: Optional[str]
    already_registered: bool
