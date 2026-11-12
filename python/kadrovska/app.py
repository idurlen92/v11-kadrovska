"""Tvornica aplikacije: svaki poziv ``create_app()`` dobiva svježu bazu u memoriji i vlastite servise."""

from __future__ import annotations

from importlib import resources

from fastapi import FastAPI

from kadrovska import pocetni_podaci, zaposlenici
from kadrovska.edukacije import api as edukacije_api
from kadrovska.edukacije.servis import EdukacijaService
from kadrovska.edukacije.sustav_za_edukacije import SustavZaEdukacijeKlijent
from kadrovska.platforma import auth_api, greske, postavke_api, prijava
from kadrovska.platforma.autentikacija import TokenStore
from kadrovska.platforma.baza import Baza
from kadrovska.platforma.cors import CorsMiddleware
from kadrovska.platforma.kontekst import Kontekst
from kadrovska.platforma.korisnici import KorisnikService
from kadrovska.platforma.lozinke import LozinkaHasher
from kadrovska.platforma.pohrana import Pohrana
from kadrovska.platforma.posta import PostaService
from kadrovska.platforma.reset_lozinke import ResetLozinkeService
from kadrovska.platne_liste import api as platne_liste_api
from kadrovska.postavke import Postavke
from kadrovska.zahtjevi import api as zahtjevi_api
from kadrovska.zahtjevi import ispis
from kadrovska.zahtjevi.servis import ZahtjevService


def create_app(postavke: Postavke | None = None) -> FastAPI:
    postavke = postavke or Postavke()
    baza = Baza()
    baza.skripta(resources.files("kadrovska").joinpath("shema.sql").read_text(encoding="utf-8"))
    pohrana = Pohrana(postavke)
    hasher = LozinkaHasher()
    korisnici = KorisnikService(baza, hasher)
    posta = PostaService(pohrana, postavke)
    kontekst = Kontekst(
        postavke=postavke,
        baza=baza,
        pohrana=pohrana,
        tokeni=TokenStore(),
        hasher=hasher,
        korisnici=korisnici,
        posta=posta,
        reset=ResetLozinkeService(baza, korisnici, posta),
        zahtjevi=ZahtjevService(baza),
        edukacije=EdukacijaService(baza, SustavZaEdukacijeKlijent()),
    )
    pocetni_podaci.ucitaj(kontekst)

    app = FastAPI(title=postavke.naziv, version="1.0.0")
    app.state.kontekst = kontekst
    app.state.baza = baza
    greske.registriraj(app)
    app.add_middleware(CorsMiddleware, dopusteni_origini=postavke.cors_allowed_origins)
    for router in (
        prijava.router,
        auth_api.router,
        postavke_api.router,
        zaposlenici.router,
        zahtjevi_api.router,
        ispis.router,
        platne_liste_api.router,
        edukacije_api.router,
    ):
        app.include_router(router)
    return app
