with active_items as (select id,name,price from items where active=1)
select i.id,i.name,sum(o.qty) over (partition by i.id order by o.created rows between unbounded preceding and current row) as running_qty
from active_items as i left join orders as o on o.item_id=i.id
where i.price between 10 and 20
order by i.name desc limit 5 offset 10;
