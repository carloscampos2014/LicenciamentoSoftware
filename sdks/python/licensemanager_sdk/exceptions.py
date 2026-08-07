class LicenseManagerException(Exception):
    """Lançada quando a API retorna um erro HTTP."""

    def __init__(self, status_code: int, response_body: str) -> None:
        super().__init__(f"LicenseManager API error {status_code}: {response_body}")
        self.status_code = status_code
        self.response_body = response_body
