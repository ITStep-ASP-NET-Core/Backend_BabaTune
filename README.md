# 🎵 BabaTune

Backend for a music streaming service built on **ASP.NET Core Web API**.

## Stack

- **.NET / ASP.NET Core Web API**
- **Entity Framework Core** — data access
- **JWT** — access/refresh tokens
- **Argon2id** — password hashing (Konscious.Security.Cryptography)
- **Firebase Storage** — audio and cover storage
- **xUnit + Moq** — tests

## Architecture

Layered architecture:

```
Domain          — entities, enums
Application     — services, DTOs, interfaces, mappers
Infrastructure  — repositories, EF Core, Firebase Storage
WebApi          — controllers
```

Patterns: **Repository + Unit of Work**, DTO mappers, a unified `Result` for error handling without exceptions.

## Main Modules

- **Auth** — registration, login, refresh/logout tokens
- **Users** — profile, search, avatar
- **Songs** — CRUD, filters, top by listens (day/week/month)
- **Albums / Playlists** — including automatic creation of the "Liked" playlist and track reordering
- **Genres / Categories** — reference lists with images
- **Subscriptions** — subscriptions to authors
- **Notices** — notifications
- **Listen History** — listening history and track popularity ranking

## Implementation Notes

- Media files (audio, covers) are uploaded to the cloud; only the link is stored in the database
- If saving fails, files already uploaded to the cloud are deleted (rollback)
- Default covers are never deleted from storage
- Pagination — a single `PagedResult<T>` for all lists
