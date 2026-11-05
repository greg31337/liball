CREATE TABLE IF NOT EXISTS items (id integer PRIMARY KEY, name text NOT NULL, price real DEFAULT 0) STRICT;
CREATE INDEX IF NOT EXISTS idx_items_name ON items (name COLLATE nocase DESC) WHERE name IS NOT NULL;
PRAGMA cache_size = -2000;
VACUUM main INTO 'backup.db';
