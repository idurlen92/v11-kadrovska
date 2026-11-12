"""Potvrda zahtjeva za godišnji odmor za ispis (HTML)."""

from __future__ import annotations

from datetime import date
from html import escape

from fastapi import APIRouter
from fastapi.responses import HTMLResponse

from kadrovska.platforma.autentikacija import Prijavljeni
from kadrovska.platforma.greske import ApiGreska
from kadrovska.platforma.kontekst import Ctx

router = APIRouter()


@router.get("/ispis/zahtjevi/{zahtjev_id}", response_class=HTMLResponse)
def potvrda(zahtjev_id: int, korisnik: Prijavljeni, ctx: Ctx) -> HTMLResponse:
    z = ctx.zahtjevi.pronadi(zahtjev_id)
    if z is None or not (korisnik.is_admin or z["zaposlenik"] == korisnik.username):
        raise ApiGreska.nije_pronadeno("Zahtjev nije pronađen.")
    sadrzaj = f"""<!DOCTYPE html>
<html lang="hr">
<head>
<meta charset="UTF-8">
<title>Zahtjev za godišnji odmor br. {z["id"]}</title>
<style>
body {{ font-family: system-ui, sans-serif; max-width: 40rem; margin: 2rem auto; }}
dt {{ font-weight: bold; margin-top: .5rem; }}
</style>
</head>
<body>
<h1>{_tekst(ctx.postavke.naziv)}</h1>
<h2>Zahtjev za godišnji odmor br. {z["id"]}</h2>
<dl>
<dt>Zaposlenik</dt><dd>{_tekst(z["zaposlenik"])}</dd>
<dt>Razdoblje</dt><dd>{_datum(z["od"])} – {_datum(z["do"])}</dd>
<dt>Razlog</dt><dd>{_tekst(z["razlog"])}</dd>
<dt>Status</dt><dd>{_tekst(z["status"])}</dd>
</dl>
<p>Potpis zaposlenika: ____________________</p>
<p>Odobrio (voditelj): ____________________</p>
</body>
</html>
"""
    odgovor = HTMLResponse(content=sadrzaj)
    odgovor.headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'"
    return odgovor


def _datum(vrijednost: str | None) -> str:
    return date.fromisoformat(vrijednost).strftime("%d.%m.%Y.") if vrijednost else ""


def _tekst(vrijednost: str | None) -> str:
    return "" if vrijednost is None else escape(vrijednost)
