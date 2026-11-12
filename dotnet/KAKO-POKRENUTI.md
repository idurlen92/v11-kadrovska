# Kadrovska (.NET 10) – kako pokrenuti

## Preduvjeti

- .NET SDK 10.0 (`dotnet --version`)
- Visual Studio 2026, JetBrains Rider ili VS Code s ekstenzijom C# Dev Kit

## Pokretanje

```bash
dotnet run --project src/Kadrovska.Api
```

Aplikacija radi na <http://localhost:5080> (profil `http` u `src/Kadrovska.Api/Properties/launchSettings.json`).

- Početna stranica: <http://localhost:5080/>
- Web prijava: <http://localhost:5080/prijava>
- Provjera rada: <http://localhost:5080/api/health>

U Visual Studiju otvori `Kadrovska.sln`, za početni projekt odaberi `Kadrovska.Api` i profil `http`.
U VS Codeu koristi *Run and Debug* (konfiguracija iz `.vscode/launch.json`).

Baza je SQLite u memoriji – pri svakom pokretanju stvara se iznova s početnim podacima. Dokumenti (platne liste) i
„poslane” e-poruke spremaju se u `src/Kadrovska.Api/data/dokumenti/` i `src/Kadrovska.Api/data/posta/`
(ključevi `App:StorageDir` i `App:MailboxDir` u `appsettings.json`).

## Korisnici

| Korisničko ime | Lozinka | Uloga |
|---|---|---|
| `ana` | `Ana12345!` | KORISNIK (zaposlenik) |
| `marko` | `Marko12345!` | KORISNIK (zaposlenik) |
| `voditelj` | `Ovlasteni12345!` | VODITELJ |
| `admin` | `Admin12345!` | ADMIN |

API prijava: `POST /api/auth/login` s tijelom `{"username":"ana","password":"Ana12345!"}`. Token iz odgovora
šalje se u zaglavlju `Authorization: Bearer <token>`. Primjeri svih zahtjeva su u `http/kadrovska.http`.

## Glavni endpointi

| Metoda i putanja | Opis |
|---|---|
| `GET /api/zaposlenici?sort=prezime&smjer=asc` | imenik zaposlenika (`sort`: ime, prezime, odjel) |
| `POST /api/zahtjevi` | zahtjev za godišnji `{ "od", "do", "razlog", "oib" }` (datumi `yyyy-MM-dd`) |
| `GET /api/zahtjevi/moji` | zahtjevi prijavljenog zaposlenika |
| `DELETE /api/zahtjevi/{id}` | povlačenje zahtjeva |
| `GET /ispis/zahtjevi/{id}` | HTML potvrda zahtjeva za ispis |
| `GET /api/platne-liste/moje` | platne liste prijavljenog zaposlenika |
| `GET /api/platne-liste/{id}?format=json` ili `format=pdf` | prikaz platne liste |
| `GET /api/platne-liste/preuzmi?datoteka=…` | preuzimanje datoteke platne liste |
| `GET /api/edukacije` | interne edukacije sa slobodnim mjestima |
| `POST /api/edukacije/{id}/prijava` | prijava na edukaciju |
| `POST /api/edukacije/uvoz` | uvoz edukacija iz XML-a (VODITELJ, ADMIN) |
| `GET`, `POST /api/postavke` | postavke obavijesti (YAML) |
| `POST /api/auth/register`, `/api/auth/reset-zahtjev`, `/api/auth/reset` | registracija i obnova lozinke |

## Testovi

```bash
dotnet test                                           # svi testovi
dotnet test --filter "Category=Funkcionalno"          # funkcionalni testovi
dotnet test --filter "Category=Primjer"               # primjer testa
dotnet test --collect:"XPlat Code Coverage"           # pokrivenost koda (TestResults/**/coverage.cobertura.xml)
```

Testovi pokreću aplikaciju u memoriji (`WebApplicationFactory`), svaka testna klasa sa svojom bazom i privremenim
mapama – aplikacija ne mora biti pokrenuta.
