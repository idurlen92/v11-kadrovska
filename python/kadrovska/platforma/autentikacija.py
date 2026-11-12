"""Tokeni prijave i određivanje prijavljenog korisnika.

Token se šalje u zaglavlju ``Authorization: Bearer <token>`` ili u kolačiću ``SESSION`` (prijava obrascem).
Endpoint traži prijavu tako da ima parametar tipa :data:`Prijavljeni`.
"""

from __future__ import annotations

import secrets
import threading
from dataclasses import dataclass
from typing import Annotated

from fastapi import Depends, Request

from kadrovska.platforma.greske import ApiGreska

KOLACIC = "SESSION"


@dataclass(frozen=True)
class PrijavljeniKorisnik:
    id: int
    username: str
    uloga: str

    @property
    def is_admin(self) -> bool:
        return self.uloga == "ADMIN"

    def ima_ulogu(self, *uloge: str) -> bool:
        return self.uloga in uloge


class TokenStore:
    """Tokeni prijave čuvaju se u memoriji (token → korisnik)."""

    def __init__(self) -> None:
        self._tokeni: dict[str, PrijavljeniKorisnik] = {}
        self._brava = threading.Lock()

    def izdaj(self, korisnik: PrijavljeniKorisnik) -> str:
        token = secrets.token_urlsafe(32)
        with self._brava:
            self._tokeni[token] = korisnik
        return token

    def korisnik(self, token: str | None) -> PrijavljeniKorisnik | None:
        if not token:
            return None
        with self._brava:
            return self._tokeni.get(token)


def token_iz_zahtjeva(request: Request) -> str | None:
    zaglavlje = request.headers.get("authorization")
    if zaglavlje and zaglavlje[:7].lower() == "bearer ":
        token = zaglavlje[7:].strip()
        return token or None
    kolacic = request.cookies.get(KOLACIC, "").strip()
    return kolacic or None


def prijavljeni_korisnik(request: Request) -> PrijavljeniKorisnik:
    tokeni: TokenStore = request.app.state.kontekst.tokeni
    korisnik = tokeni.korisnik(token_iz_zahtjeva(request))
    if korisnik is None:
        raise ApiGreska(401, "Potrebna je prijava.")
    return korisnik


Prijavljeni = Annotated[PrijavljeniKorisnik, Depends(prijavljeni_korisnik)]
