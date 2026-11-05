select region, sum(amount) as total
from sales
where amount > 0
group by region
having sum(amount) > 100
order by total desc;
