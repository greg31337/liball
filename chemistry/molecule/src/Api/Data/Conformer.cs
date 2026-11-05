using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides conformer data and operations for molecular graphs.
/// </summary>
public class Conformer
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The energy.
    /// </summary>
    public double Energy;
    /// <summary>
    /// Whether the value has energy.
    /// </summary>
    public bool HasEnergy;
    /// <summary>
    /// The coordinates.
    /// </summary>
    public List<Coordinate3D> Coordinates;

    /// <summary>
    /// Creates an empty conformer.
    /// </summary>
    /// <returns>A conformer without coordinate slots.</returns>
    internal static Conformer CreateEmpty()
    {
        return new Conformer(0);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Conformer"/> class.
    /// </summary>
    /// <param name="atomCount">The atom count.</param>
    public Conformer(int atomCount)
    {
        Id = -1;
        Name = "";
        Energy = 0.0;
        HasEnergy = false;
        Coordinates = new List<Coordinate3D>();
        EnsureCoordinateCount(atomCount);
    }

    /// <summary>
    /// Ensure coordinate count.
    /// </summary>
    /// <param name="atomCount">The atom count.</param>
    internal void EnsureCoordinateCount(int atomCount)
    {
        if (atomCount < 0)
        {
            return;
        }

        while (Coordinates.Count < atomCount)
        {
            Coordinates.Add(Coordinate3D.CreateEmpty());
        }
    }

    /// <summary>
    /// Sets the atom coordinate.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="z">The z.</param>
    internal void SetAtomCoordinate(int atomId, double x, double y, double z)
    {
        if (atomId < 0)
        {
            return;
        }

        EnsureCoordinateCount(atomId + 1);
        Coordinates[atomId].Set(x, y, z);
    }

    /// <summary>
    /// Returns the atom coordinate.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The resulting coordinate 3D.</returns>
    internal Coordinate3D GetAtomCoordinate(int atomId)
    {
        if (atomId < 0 || atomId >= Coordinates.Count)
        {
            return Coordinate3D.CreateEmpty();
        }

        return Coordinates[atomId];
    }

    /// <summary>
    /// Determines whether the value has atom coordinate.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasAtomCoordinate(int atomId)
    {
        if (atomId < 0 || atomId >= Coordinates.Count)
        {
            return false;
        }

        return Coordinates[atomId].IsSet;
    }

    /// <summary>
    /// Clears the atom coordinate.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    internal void ClearAtomCoordinate(int atomId)
    {
        if (atomId < 0 || atomId >= Coordinates.Count)
        {
            return;
        }

        Coordinates[atomId].Clear();
    }

    internal void RemoveAtomCoordinate(int atomId)
    {
        if (atomId < 0 || atomId >= Coordinates.Count)
        {
            return;
        }

        Coordinates.RemoveAt(atomId);
    }

    /// <summary>
    /// Counts the assigned coordinates.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int CountAssignedCoordinates()
    {
        int count = 0;
        int index = 0;
        while (index < Coordinates.Count)
        {
            if (Coordinates[index].IsSet)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting conformer.</returns>
    internal Conformer Clone()
    {
        Conformer conformer = Conformer.CreateEmpty();
        conformer.Id = Id;
        conformer.Name = Name;
        conformer.Energy = Energy;
        conformer.HasEnergy = HasEnergy;

        int index = 0;
        while (index < Coordinates.Count)
        {
            conformer.Coordinates.Add(Coordinates[index].Clone());
            index = index + 1;
        }

        return conformer;
    }
}

