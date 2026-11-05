namespace fake;

internal class FakeProviders
{
    internal FakeRandom RandomValue;
    internal PrimitiveProvider PrimitiveValue;
    internal TextProvider TextValue;
    internal TemplateProvider TemplateValue;
    internal ChecksumProvider ChecksumValue;
    internal IdentifierProvider IdentifierValue;
    internal DateProvider DateValue;
    internal LocaleProvider LocaleValue;
    internal PersonProvider PersonValue;
    internal AddressProvider AddressValue;
    internal InternetProvider InternetValue;
    internal PhoneProvider PhoneValue;
    internal DeviceProvider DeviceValue;
    internal FinanceProvider FinanceValue;
    internal CompanyProvider CompanyValue;
    internal CommerceProvider CommerceValue;
    internal SystemProvider SystemValue;
    internal DatabaseProvider DatabaseValue;
    internal GitProvider GitValue;
    internal ColorProvider ColorValue;
    internal VehicleProvider VehicleValue;
    internal AirlineProvider AirlineValue;
    internal BookProvider BookValue;
    internal MediaProvider MediaValue;
    internal FoodProvider FoodValue;
    internal AnimalProvider AnimalValue;
    internal ScienceProvider ScienceValue;
    internal HealthProvider HealthValue;
    internal EducationProvider EducationValue;
    internal CultureProvider CultureValue;
    internal ProfileProvider ProfileValue;
    internal SchemaGenerator SchemaValue;
    internal EdgeProvider EdgeValue;
    internal ExportProvider ExportValue;

    internal FakeProviders(GenerationContext context, Fake owner)
    {
        RandomValue = FakeRandom.FromContext(context);
        PrimitiveValue = new PrimitiveProvider(context);
        TextValue = new TextProvider(context);
        TemplateValue = new TemplateProvider(context);
        ChecksumValue = new ChecksumProvider(context);
        IdentifierValue = new IdentifierProvider(context);
        DateValue = new DateProvider(context);
        LocaleValue = new LocaleProvider(context);
        PersonValue = new PersonProvider(context);
        AddressValue = new AddressProvider(context);
        InternetValue = new InternetProvider(context);
        PhoneValue = new PhoneProvider(context);
        DeviceValue = new DeviceProvider(context);
        FinanceValue = new FinanceProvider(context);
        CompanyValue = new CompanyProvider(context);
        CommerceValue = new CommerceProvider(context);
        SystemValue = new SystemProvider(context);
        DatabaseValue = new DatabaseProvider(context);
        GitValue = new GitProvider(context);
        ColorValue = new ColorProvider(context);
        VehicleValue = new VehicleProvider(context);
        AirlineValue = new AirlineProvider(context);
        BookValue = new BookProvider(context);
        MediaValue = new MediaProvider(context);
        FoodValue = new FoodProvider(context);
        AnimalValue = new AnimalProvider(context);
        ScienceValue = new ScienceProvider(context);
        HealthValue = new HealthProvider(context);
        EducationValue = new EducationProvider(context);
        CultureValue = new CultureProvider(context);
        ProfileValue = new ProfileProvider(context, owner);
        SchemaValue = new SchemaGenerator(context, owner);
        EdgeValue = new EdgeProvider(context);
        ExportValue = new ExportProvider(context);
    }
}
