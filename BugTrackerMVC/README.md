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
