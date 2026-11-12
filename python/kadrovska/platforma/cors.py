"""CORS za frontend aplikaciju (npr. Vite na ``http://localhost:5173``) koja API poziva s kolačićem ili
tokenom. Odgovara i na preflight zahtjeve (``OPTIONS``)."""

from __future__ import annotations

from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint
from starlette.requests import Request
from starlette.responses import Response
from starlette.types import ASGIApp

METODE = "GET, POST, PUT, DELETE, OPTIONS"
ZAGLAVLJA = "Authorization, Content-Type"


class CorsMiddleware(BaseHTTPMiddleware):
    def __init__(self, app: ASGIApp, dopusteni_origini: list[str]) -> None:
        super().__init__(app)
        self._dopusteni_origini = list(dopusteni_origini)

    async def dispatch(self, request: Request, call_next: RequestResponseEndpoint) -> Response:
        origin = request.headers.get("origin")
        if origin is None:
            return await call_next(request)
        preflight = request.method == "OPTIONS" and "access-control-request-method" in request.headers
        if self._dopusten(origin):
            if preflight:
                odgovor = Response(status_code=204)
                odgovor.headers["Access-Control-Allow-Methods"] = METODE
                odgovor.headers["Access-Control-Allow-Headers"] = ZAGLAVLJA
                odgovor.headers["Access-Control-Max-Age"] = "3600"
            else:
                odgovor = await call_next(request)
            odgovor.headers["Access-Control-Allow-Origin"] = origin
            odgovor.headers["Access-Control-Allow-Credentials"] = "true"
        elif preflight:
            odgovor = Response(status_code=403)
        else:
            odgovor = await call_next(request)
        odgovor.headers.append("Vary", "Origin")
        return odgovor

    def _dopusten(self, origin: str) -> bool:
        return origin.startswith(("http://", "https://"))
