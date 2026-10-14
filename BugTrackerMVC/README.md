# BugTrackerMVC – dokumentacja projektu

## Wymagania
- Visual Studio 2022 (lub nowsze) z workloadem **ASP.NET and web development**.
- .NET SDK 8.x.
- SQLite (używane przez EF Core – baza to plik, nic nie trzeba instalować osobno).

## Instalacja (Visual Studio)
1. Otwórz rozwiązanie `BugTrackerMVC.sln`.
2. Utwórz/odśwież bazę w **Package Manager Console**:
   - `Tools → NuGet Package Manager → Package Manager Console`
   - wykonaj:
     ```powershell
     Add-Migration InitialCreate   # tylko jeśli wprowadzałeś zmiany i nie masz migracji
     Update-Database
     ```
3. Uruchom aplikację (F5 lub Ctrl+F5).

## Konfiguracja (połączenie z bazą)
Plik: `BugTrackerMVC/appsettings.json`
- `DefaultConnection`: `Data Source=app.db`


### Konta testowe
Przy starcie aplikacja robi seed danych (role, konta i przykładowe projekty/kategorie). Konta do testów:
- **Administrator**: `admin@demo.local` / `Admin123!`
- **Użytkownik**: `user@demo.local` / `User123!`

## Opis działania aplikacji z perspektywy użytkownika
- Użytkownik może się zarejestrować i zalogować (gotowe strony Identity).
- Po zalogowaniu tworzy zgłoszenia (tickets), edytuje je, usuwa i dodaje komentarze.
- Zwykły użytkownik widzi tylko swoje zgłoszenia, a administrator widzi wszystkie.
- Administrator ma dodatkowo zarządzanie projektami, kategoriami oraz użytkownikami/rolami.

## Walidacja zgłoszeń i testy

MVC oraz `POST /api/tickets` i `PUT /api/tickets/{id}` korzystają ze wspólnego modelu
`TicketInput`: tytuł musi mieć 5–120 znaków, opis 20–4000 znaków, a status i priorytet
muszą być zdefiniowanymi wartościami enum. Projekt i kategoria muszą istnieć.
API zwraca błędy walidacji jako HTTP 400 (`application/problem+json`) ze słownikiem
`errors`, którego klucze wskazują nieprawidłowe pola. MVC wyświetla błędy w formularzu.

Uruchomienie z katalogu rozwiązania, przy użyciu .NET SDK 8.x:

```bash
dotnet restore BugTrackerMVC.sln
dotnet build BugTrackerMVC.sln --configuration Release --no-restore
dotnet test BugTrackerMVC.sln --configuration Release --no-build
```

Testy integracyjne uruchamiają aplikację przez `WebApplicationFactory` z osobną bazą
SQLite w pamięci i włączonymi kluczami obcymi. Podmieniają uwierzytelnianie wyłącznie
w hoście testowym; właściwa autoryzacja, walidacja MVC/API i zapis przez EF Core
pozostają aktywne. Obejmują wartości graniczne, błędne pola i relacje, brak zapisu
po błędzie oraz dostęp właściciela i administratora. Workflow GitHub Actions
uruchamia build i testy dla pull requestów do `master` oraz zmian na `master`.
