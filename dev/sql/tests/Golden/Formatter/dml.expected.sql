INSERT INTO items (id, name) VALUES (1, 'a'), (2, 'b') ON CONFLICT(id) DO UPDATE SET name = excluded.name RETURNING id, name;
UPDATE items SET name = 'x', price = price + 1 WHERE id = 1 RETURNING *;
DELETE FROM items WHERE id IN (SELECT id FROM old_items) RETURNING id;
