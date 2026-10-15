USE LiquidStockDB;
GO

CREATE TABLE Customer (
    customer_ID INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    email NVARCHAR(255) NOT NULL UNIQUE,
    phone_number NVARCHAR(30) NULL,
    date_joined DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);

CREATE TABLE Staff (
    staff_ID INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    role NVARCHAR(50) NOT NULL,
    username NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1
);

CREATE TABLE Drink (
    drink_ID INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    category NVARCHAR(100) NOT NULL,
    current_price DECIMAL(10,2) NOT NULL,
    min_price DECIMAL(10,2) NOT NULL,
    max_price DECIMAL(10,2) NOT NULL,
    stock_quantity INT NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_Drink_StockQuantity
        CHECK (stock_quantity >= 0),

    CONSTRAINT CK_Drink_MinPrice
        CHECK (min_price >= 0),

    CONSTRAINT CK_Drink_PriceRange
        CHECK (max_price >= min_price),

    CONSTRAINT CK_Drink_CurrentPrice
        CHECK (current_price BETWEEN min_price AND max_price)
);

-- [Order] needs brackets because ORDER is a SQL keyword.

CREATE TABLE [Order] (
    order_ID INT IDENTITY(1,1) PRIMARY KEY,
    customer_ID INT NOT NULL,
    staff_ID INT NOT NULL,
    order_date DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    total_amount DECIMAL(12,2) NOT NULL DEFAULT 0 CHECK (total_amount >= 0),
    status NVARCHAR(30) NOT NULL DEFAULT 'Pending',

    CONSTRAINT FK_Order_Customer
        FOREIGN KEY (customer_ID) REFERENCES Customer(customer_ID),

    CONSTRAINT FK_Order_Staff
        FOREIGN KEY (staff_ID) REFERENCES Staff(staff_ID),

    CONSTRAINT CK_Order_Status
        CHECK (status IN ('Pending', 'Completed', 'Cancelled'))
);

CREATE TABLE Order_Item (
    order_item_ID INT IDENTITY(1,1) PRIMARY KEY,
    order_ID INT NOT NULL,
    drink_ID INT NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10,2) NOT NULL CHECK (unit_price >= 0),

    subtotal AS (quantity * unit_price) PERSISTED,

    CONSTRAINT FK_OrderItem_Order
        FOREIGN KEY (order_ID) REFERENCES [Order](order_ID),

    CONSTRAINT FK_OrderItem_Drink
        FOREIGN KEY (drink_ID) REFERENCES Drink(drink_ID),

    CONSTRAINT UQ_OrderItem_OrderDrink
        UNIQUE (order_ID, drink_ID)
);

CREATE TABLE Inventory (
    inventory_ID INT IDENTITY(1,1) PRIMARY KEY,
    drink_ID INT NOT NULL UNIQUE,
    quantity INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    last_updated DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Inventory_Drink
        FOREIGN KEY (drink_ID) REFERENCES Drink(drink_ID)
);

CREATE TABLE History (
    history_ID INT IDENTITY(1,1) PRIMARY KEY,
    drink_ID INT NOT NULL,
    current_price DECIMAL(10,2) NOT NULL CHECK (current_price >= 0),
    old_price DECIMAL(10,2) NOT NULL CHECK (old_price >= 0),
    new_price DECIMAL(10,2) NOT NULL CHECK (new_price >= 0),

    CONSTRAINT FK_History_Drink
        FOREIGN KEY (drink_ID) REFERENCES Drink(drink_ID)
);
GO