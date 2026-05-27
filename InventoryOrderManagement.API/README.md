# Inventory & Order Management API

A RESTful API built with **ASP.NET Core 8** and **PostgreSQL** for managing inventory, products, warehouses, purchase orders, and sales orders across a supply chain system.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [API Endpoints](#api-endpoints)
- [Business Logic](#business-logic)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running Migrations](#running-migrations)
- [Known Limitations & Future Improvements](#known-limitations--future-improvements)

---

## Overview

This API covers the full supply chain lifecycle:

- Manage **products**, **categories**, and **suppliers**
- Track **inventory** levels across multiple **warehouses** and **locations**
- Process **purchase orders** (inbound from suppliers)
- Process **sales orders** (outbound to customers) with real-time **stock validation**
- Manage **users** and **roles** (data model ready; auth not yet enforced)

---

## Tech Stack

| Layer           | Technology                              |
|-----------------|-----------------------------------------|
| Framework       | ASP.NET Core 8 Web API                  |
| Language        | C# (.NET 8)                             |
| Database        | PostgreSQL                              |
| ORM             | Entity Framework Core 8 (Code-First)    |
| API Docs        | Swagger / OpenAPI (Swashbuckle)         |
| JSON Handling   | System.Text.Json (cycle-safe)           |

**NuGet Packages:**
- `Npgsql.EntityFrameworkCore.PostgreSQL` v8.0.0
- `Microsoft.EntityFrameworkCore.Design` v8.0.0
- `Microsoft.EntityFrameworkCore.InMemory` v8.0.0
- `Swashbuckle.AspNetCore` v6.6.2
- `Microsoft.AspNetCore.OpenApi` v8.0.22

---

## Project Structure

```
InventoryOrderManagement.API/
├── Controllers/              # 13 API controllers
├── Data/
│   └── AppDbContext.cs       # EF Core DbContext (13 DbSets)
├── Models/
│   ├── *.cs                  # Domain entities
│   └── DTOs/                 # Create / Update / Response DTOs
├── Services/
│   └── InventoryService.cs   # Stock validation & inventory queries
├── Migrations/               # EF Core code-first migrations
├── Program.cs                # App configuration & DI registration
└── appsettings.json          # Connection string & logging config
```

---

## Domain Model

### User Management
| Entity | Key Fields |
|--------|-----------|
| `User` | UserId, FullName, Email, PasswordHash (SHA256), RoleId |
| `Role` | RoleId, Name (unique) |

### Product Catalog
| Entity | Key Fields |
|--------|-----------|
| `Product` | ProductId, Name, SKU (unique), UnitPrice, CategoryId, SupplierId, IsActive |
| `Category` | CategoryId, Name (unique), Description |
| `Supplier` | SupplierId, Name, ContactPerson, Email, Phone, Address |

### Inventory
| Entity | Key Fields |
|--------|-----------|
| `Inventory` | InventoryId, ProductId, WarehouseId, LocationId, Quantity, ReorderLevel |
| `Warehouse` | WarehouseId, Name, Address |
| `Location` | LocationId, WarehouseId, Code, Description |

### Orders
| Entity | Key Fields |
|--------|-----------|
| `PurchaseOrder` | PurchaseOrderId, SupplierId, OrderDate, Status, TotalAmount |
| `PurchaseOrderItem` | PurchaseOrderItemId, PurchaseOrderId, ProductId, Quantity, UnitPrice |
| `SalesOrder` | SalesOrderId, CustomerId, OrderDate, Status, TotalAmount |
| `SalesOrderItem` | SalesOrderItemId, SalesOrderId, ProductId, Quantity, UnitPrice |
| `Customer` | CustomerId, FullName, Email, Phone, Address |

---

## API Endpoints

### Users — `/api/users`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users` | Get all users |
| GET | `/api/users/{id}` | Get user by ID |
| GET | `/api/users/email/{email}` | Get user by email |
| GET | `/api/users/role/{roleId}` | Get users by role |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |

### Roles — `/api/roles`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/roles` | Get all roles |
| GET | `/api/roles/{id}` | Get role by ID |
| POST | `/api/roles` | Create role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role (validates no users assigned) |

### Categories — `/api/categories`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/categories` | Get all categories |
| GET | `/api/categories/{id}` | Get category by ID |
| POST | `/api/categories` | Create category |
| PUT | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category (validates no linked products) |

### Suppliers — `/api/suppliers`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/suppliers` | Get all suppliers |
| GET | `/api/suppliers/{id}` | Get supplier by ID |
| POST | `/api/suppliers` | Create supplier |
| PUT | `/api/suppliers/{id}` | Update supplier |
| DELETE | `/api/suppliers/{id}` | Delete supplier (validates no products/purchase orders) |

### Products — `/api/products`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/products` | Get all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create product (validates unique SKU) |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |

### Warehouses — `/api/warehouses`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/warehouses` | Get all warehouses |
| GET | `/api/warehouses/{id}` | Get warehouse by ID |
| POST | `/api/warehouses` | Create warehouse |
| PUT | `/api/warehouses/{id}` | Update warehouse |
| DELETE | `/api/warehouses/{id}` | Delete warehouse (validates no locations/inventory) |

### Locations — `/api/locations`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/locations` | Get all locations |
| GET | `/api/locations/{id}` | Get location by ID |
| GET | `/api/locations/warehouse/{warehouseId}` | Get locations by warehouse |
| POST | `/api/locations` | Create location |
| PUT | `/api/locations/{id}` | Update location |
| DELETE | `/api/locations/{id}` | Delete location |

### Inventories — `/api/inventories`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/inventories` | Get all inventory records |
| GET | `/api/inventories/{id}` | Get inventory by ID |
| GET | `/api/inventories/product/{productId}` | Get inventory by product |
| GET | `/api/inventories/warehouse/{warehouseId}` | Get inventory by warehouse |
| POST | `/api/inventories` | Create inventory record |
| PUT | `/api/inventories/{id}` | Update inventory |
| DELETE | `/api/inventories/{id}` | Delete inventory record |

### Customers — `/api/customers`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/customers` | Get all customers |
| GET | `/api/customers/{id}` | Get customer by ID |
| POST | `/api/customers` | Create customer |
| PUT | `/api/customers/{id}` | Update customer |
| DELETE | `/api/customers/{id}` | Delete customer (validates no sales orders) |

### Purchase Orders — `/api/purchaseorders`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/purchaseorders` | Get all purchase orders |
| GET | `/api/purchaseorders/{id}` | Get purchase order by ID |
| GET | `/api/purchaseorders/supplier/{supplierId}` | Get by supplier |
| GET | `/api/purchaseorders/status/{status}` | Get by status |
| POST | `/api/purchaseorders` | Create purchase order |
| PUT | `/api/purchaseorders/{id}` | Update purchase order |
| DELETE | `/api/purchaseorders/{id}` | Delete purchase order |
| POST | `/api/purchaseorders/{id}/items` | Add item to order |
| DELETE | `/api/purchaseorders/{id}/items/{itemId}` | Remove item from order |

### Purchase Order Items — `/api/purchaseorderitems`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/purchaseorderitems` | Get all items |
| GET | `/api/purchaseorderitems/{id}` | Get item by ID |
| GET | `/api/purchaseorderitems/order/{orderId}` | Get items by order |
| GET | `/api/purchaseorderitems/product/{productId}` | Get items by product |
| POST | `/api/purchaseorderitems` | Create item |
| PUT | `/api/purchaseorderitems/{id}` | Update item |
| DELETE | `/api/purchaseorderitems/{id}` | Delete item |

### Sales Orders — `/api/salesorders`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/salesorders` | Get all sales orders |
| GET | `/api/salesorders/{id}` | Get sales order by ID |
| GET | `/api/salesorders/customer/{customerId}` | Get by customer |
| GET | `/api/salesorders/status/{status}` | Get by status |
| POST | `/api/salesorders` | Create sales order |
| PUT | `/api/salesorders/{id}` | Update sales order |
| DELETE | `/api/salesorders/{id}` | Delete sales order |
| POST | `/api/salesorders/{id}/items` | Add item to order |
| DELETE | `/api/salesorders/{id}/items/{itemId}` | Remove item from order |

### Sales Order Items — `/api/salesorderitems`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/salesorderitems` | Get all items |
| GET | `/api/salesorderitems/{id}` | Get item by ID |
| GET | `/api/salesorderitems/order/{orderId}` | Get items by order |
| GET | `/api/salesorderitems/product/{productId}` | Get items by product |
| POST | `/api/salesorderitems` | Create item (validates stock) |
| PUT | `/api/salesorderitems/{id}` | Update item |
| DELETE | `/api/salesorderitems/{id}` | Delete item |

---

## Business Logic

### Stock Validation (`InventoryService`)
Before a sales order item is created, the system validates stock availability:
- Aggregates quantity across all warehouse locations for a product
- Returns `400 Bad Request` with a descriptive message if insufficient stock
- Prevents negative inventory at both global and warehouse levels

### Auto-Calculated Order Totals
Order `TotalAmount` is automatically recalculated whenever items are **added**, **updated**, or **removed**.

### Referential Integrity (Application-Level)
Before deleting any parent record, the API checks for dependent child records:
- Cannot delete a **Supplier** if it has linked products or purchase orders
- Cannot delete a **Category** if it has linked products
- Cannot delete a **Warehouse** if it has locations or inventory records
- Cannot delete a **Customer** if it has sales orders
- Cannot delete a **Role** if users are assigned to it

### Unique Constraints
Enforced at both DB index and application level:
- `User.Email`
- `Product.SKU`
- `Category.Name`
- `Role.Name`

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (v13+)

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd InventoryOrderManagement.API
   ```

2. **Configure the database connection**  
   Update `appsettings.json` with your PostgreSQL credentials (see [Configuration](#configuration)).

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open Swagger UI**  
   Navigate to `https://localhost:{port}/swagger` to explore and test the API.

---

## Configuration

Update `appsettings.json` with your PostgreSQL connection details:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_sys;Username=your_user;Password=your_password"
  }
}
```

> **Note:** For production, store credentials in environment variables or a secrets manager, not in `appsettings.json`.

---

## Running Migrations

```bash
# Apply existing migrations to create the database schema
dotnet ef database update

# Add a new migration (after modifying models)
dotnet ef migrations add <MigrationName>

# Revert last migration
dotnet ef migrations remove
```

---

## Known Limitations & Future Improvements

| Area | Current State | Planned Improvement |
|------|--------------|---------------------|
| Authentication | None | JWT Bearer tokens |
| Authorization | None | Role-based (`[Authorize(Roles="Admin")]`) |
| Password Hashing | SHA256 | BCrypt / Argon2 |
| Pagination | Not implemented | Cursor/offset pagination on list endpoints |
| Repository Pattern | Implicit (EF Core) | Explicit repository interfaces for testability |
| Unit Tests | Not implemented | xUnit + EF Core InMemory provider |
| Secrets Management | Hardcoded in config | Environment variables / Azure Key Vault |
| API Versioning | Not implemented | URL or header-based versioning |
