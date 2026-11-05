create table if not exists items(id integer primary key, name text not null, price real default 0) strict;
create index if not exists idx_items_name on items(name collate nocase desc) where name is not null;
pragma cache_size = -2000;
vacuum main into 'backup.db';
