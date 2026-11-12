from argon2 import PasswordHasher
from argon2.exceptions import InvalidHashError, VerificationError


class LozinkaHasher:
    """Izračun i provjera sažetka lozinke (registracija, prijava, promjena lozinke, početni podaci)."""

    def __init__(self) -> None:
        self._hasher = PasswordHasher()

    def hash(self, lozinka: str) -> str:
        return self._hasher.hash(lozinka)

    def provjeri(self, lozinka: str, spremljeni_hash: str | None) -> bool:
        if not spremljeni_hash:
            return False
        try:
            return self._hasher.verify(spremljeni_hash, lozinka)
        except (VerificationError, InvalidHashError):
            return False
