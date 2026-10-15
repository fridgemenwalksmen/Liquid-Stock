# Liquid Stock API: contract for the WinForms app

Base URL (development): `https://localhost:7278`
First time only, so HttpClient trusts the dev certificate: `dotnet dev-certs https --trust`
All bodies are JSON. Enums are strings. Money is a decimal number (e.g. 27.5).
Errors come back as `{ "title", "status", "detail" }`: show `detail` to the user.

## Drinks
| Method | URL | Body | Notes |
|---|---|---|---|
| GET | `/api/drinks` | | `?category=Beer` and `?includeInactive=true` optional |
| GET | `/api/drinks/{id}` | | 404 if missing |
| GET | `/api/drinks/{id}/price-history` | | oldest first; `?from=&to=&limit=` optional |
| POST | `/api/drinks` | `{ name, category, currentPrice, minPrice, maxPrice, stockQuantity }` | 409 if name exists |
| PUT | `/api/drinks/{id}` | `{ name, category, minPrice, maxPrice, isActive }` | |
| PUT | `/api/drinks/{id}/price` | `{ newPrice }` | 400 if outside min/max |
| POST | `/api/drinks/{id}/restock` | `{ quantity }` | adds stock |

Drink response:
```json
{ "id": 1, "name": "Coca-Cola 500ml", "category": "Soft Drink", "currentPrice": 25.00,
  "minPrice": 20.00, "maxPrice": 30.00, "stockQuantity": 40, "isActive": true,
  "previousPrice": 23.00, "trend": "Up" }
```
`trend` is `"Up"`, `"Down"`, `"Same"` or `"None"`.

## Orders
| Method | URL | Body |
|---|---|---|
| POST | `/api/orders` | `{ customerId?, staffId?, completeImmediately, items: [ { drinkId, quantity } ] }` |
| GET | `/api/orders/{id}` | |
| GET | `/api/orders` | `?status=Pending&customerId=1&from=&to=&limit=` all optional, newest first |
| PATCH | `/api/orders/{id}/status` | `{ "status": "Completed" }` or `"Cancelled"` |

- Leave out `customerId` for a walk-in. The price is always taken from the server at the moment the order is placed, so never send a price.
- 409 = not enough stock / drink unavailable / status change not allowed. Show the `detail` text.
- Allowed status changes: Pending -> Completed, Pending -> Cancelled, Completed -> Cancelled (cancelling returns stock).

Order response:
```json
{ "id": 4, "customerId": 1, "staffId": 2, "orderDate": "2026-10-15T10:15:00",
  "totalAmount": 50.00, "status": "Completed",
  "items": [ { "drinkId": 1, "drinkName": "Coca-Cola 500ml", "quantity": 2, "unitPrice": 25.00, "subtotal": 50.00 } ] }
```

## Coming later
Login (JWT) for staff/managers, report endpoints, and live price updates.
