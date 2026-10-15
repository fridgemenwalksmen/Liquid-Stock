USE LiquidStockDB;
GO


INSERT INTO Customer (name, email, phone_number, date_joined)
VALUES
    ('Liam Smith', 'liam.smith@email.com', '0825551001', '2026-10-01'),
    ('Ava Johnson', 'ava.johnson@email.com', '0825551002', '2026-10-03'),
    ('Noah Williams', 'noah.williams@email.com', '0825551003', '2026-10-05');

.
INSERT INTO Staff (name, role, username, password_hash, is_active)
VALUES
    ('Ethan Brown', 'Manager', 'ethan.manager', 'PLACEHOLDER_HASH_1', 1),
    ('Mia Davis', 'Cashier', 'mia.cashier', 'PLACEHOLDER_HASH_2', 1);


INSERT INTO Drink
    (name, category, current_price, min_price, max_price, stock_quantity, is_active)
VALUES
    ('Coca-Cola 500ml', 'Soft Drink', 25.00, 20.00, 30.00, 40, 1),
    ('Sprite 500ml', 'Soft Drink', 25.00, 20.00, 30.00, 35, 1),
    ('Still Water 500ml', 'Water', 15.00, 10.00, 20.00, 60, 1),
    ('Red Bull 250ml', 'Energy Drink', 32.00, 28.00, 38.00, 20, 1),
    ('Orange Juice 500ml', 'Juice', 28.00, 22.00, 35.00, 25, 1);


INSERT INTO Inventory (drink_ID, quantity, last_updated)
VALUES
    (1, 40, '2026-10-06 09:00:00'),
    (2, 35, '2026-10-06 09:00:00'),
    (3, 60, '2026-10-06 09:00:00'),
    (4, 20, '2026-10-06 09:00:00'),
    (5, 25, '2026-10-06 09:00:00');


INSERT INTO [Order]
    (customer_ID, staff_ID, order_date, total_amount, status)
VALUES
    (1, 2, '2026-10-06 10:15:00', 50.00, 'Completed'),
    (2, 2, '2026-10-06 11:30:00', 47.00, 'Completed'),
    (3, 1, '2026-10-06 12:05:00', 32.00, 'Pending');


INSERT INTO Order_Item (order_ID, drink_ID, quantity, unit_price)
VALUES
    (1, 1, 2, 25.00), 
    (2, 3, 1, 15.00), 
    (2, 4, 1, 32.00), 
    (3, 4, 1, 32.00); 


INSERT INTO History (drink_ID, current_price, old_price, new_price)
VALUES
    (1, 25.00, 23.00, 25.00),
    (2, 25.00, 22.00, 25.00),
    (4, 32.00, 30.00, 32.00),
    (5, 28.00, 25.00, 28.00);
GO