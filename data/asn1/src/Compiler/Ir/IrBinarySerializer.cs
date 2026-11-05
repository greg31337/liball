namespace asn1;

internal class IrBinarySerializer
{
    internal byte[] Serialize(
        LoweredCompilationIr compilation,
        AsnResourceLimits limits)
    {
        if (compilation == null)
        {
            throw new System.ArgumentNullException("compilation");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        IrTypeCollector collector = new IrTypeCollector(limits);
        collector.CollectCompilationTypes(compilation);
        return new IrBinaryWriter(limits, collector).WriteCompilation(compilation);
    }

    internal LoweredCompilationIr Deserialize(
        byte[] data,
        AsnResourceLimits limits)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        return new IrBinaryReader(data, limits).ReadCompilation();
    }
}
