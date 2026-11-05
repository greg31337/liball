insert into items(id,name) values (1,'a'),(2,'b') on conflict(id) do update set name=excluded.name returning id,name;
update items set name='x', price=price+1 where id=1 returning *;
delete from items where id in (select id from old_items) returning id;
