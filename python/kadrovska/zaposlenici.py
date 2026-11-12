"""Imenik zaposlenika, npr. ``GET /api/zaposlenici?sort=prezime&smjer=asc``."""

from __future__ import annotations

from typing import Any

from fastapi import APIRouter

from kadrovska.platforma.autentikacija import Prijavljeni
from kadrovska.platforma.kontekst import Ctx

router = APIRouter(prefix="/api/zaposlenici")


@router.get("")
def popis(korisnik: Prijavljeni, ctx: Ctx, sort: str = "prezime", smjer: str = "asc") -> list[dict[str, Any]]:
    sql = f"SELECT id, ime, prezime, odjel, email FROM zaposlenici ORDER BY {sort} {smjer}"
    return ctx.baza.upit(sql)
