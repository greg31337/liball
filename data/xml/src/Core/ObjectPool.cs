using System;
using System.Collections.Generic;

namespace xml;

internal interface IObjectPoolFactory
{
    object CreatePoolItem();
}

internal class ObjectPool
{
    internal IObjectPoolFactory Factory;
    internal Stack<object> Items;
    internal int MaximumRetained;

    internal ObjectPool(IObjectPoolFactory factory, int maximumRetained)
    {
        Guard.NotNull(factory, "factory");
        Guard.NonNegativeInt(maximumRetained, "maximumRetained");
        Factory = factory;
        Items = new Stack<object>();
        MaximumRetained = maximumRetained;
    }

    internal object RentItem()
    {
        lock (Items)
        {
            if (Items.Count > 0)
            {
                return Items.Pop();
            }
        }

        object item = Factory.CreatePoolItem();
        if (item == null)
        {
            throw new InvalidOperationException("The object pool factory returned null.");
        }

        return item;
    }

    internal bool ReturnItem(object item)
    {
        Guard.NotNull(item, "item");
        lock (Items)
        {
            foreach (object retained in Items)
            {
                if (object.ReferenceEquals(retained, item))
                {
                    return false;
                }
            }

            if (Items.Count >= MaximumRetained)
            {
                return false;
            }

            Items.Push(item);
            return true;
        }
    }

    internal int PoolCount()
    {
        lock (Items)
        {
            return Items.Count;
        }
    }

    internal void ClearPool()
    {
        lock (Items)
        {
            Items.Clear();
        }
    }
}
