# MinecraftCIEC

To run the project locally, outcomment following lines in Web.config:

<connectionStrings>
    <add name="DefaultConnection" connectionString="Data Source=PABLOWIN\SQLEXPRESS;AttachDbFilename=|DataDirectory|\aspnet-EvolutionDBContext.mdf;Initial Catalog=aspnet-EvolutionDBContext;Integrated Security=True" providerName="System.Data.SqlClient" />
    <add name="EvolutionDBContext" connectionString="Data Source=PABLOWIN\SQLEXPRESS; Initial Catalog=EvolutionDBContext; Integrated Security=True; MultipleActiveResultSets=True; AttachDbFilename=|DataDirectory|EvolutionDBContext.mdf" providerName="System.Data.SqlClient" />
</connectionStrings>

To run the project on crowdai.itu.dk, keep the lines intact
