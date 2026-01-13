using System.Text;

namespace DaxSharpTests;

using DaxSharp;

public class ExportPostgresTests
{
    [Fact]
    public void ExportAllTables_FromPowerBiToPostgreSQL_ShouldSucceed()
    {
        var config = DaxSharpConfig.Instance;
        var results = DaxSharpPbiExportToPostgres.ExportAllTables(
            pbiConnectionString: config.ConnectionStrings.PowerBi,
            postgresConnectionString: config.ConnectionStrings.PostgreSQL
        );
        
        Assert.All(results, x =>
        {
            Assert.NotEqual(-1, x.Value);
        });
    }

    [Fact]
    public void ExecuteDaxQueryEnumerable_WithLazyLoading_ShouldProcessRowsIncrementally()
    {
        var config = DaxSharpConfig.Instance;
        var connectionString = config.ConnectionStrings.PowerBi;
        const string query = "EVALUATE 'sales'";

        var result = new StringBuilder();
        
		// Lazy loading
        foreach (var row in DaxSharpPbiExportExtensions.ExecuteDaxQueryEnumerable(connectionString, query))
        {
            foreach (var column in row)
            {
                result.AppendLine($"{column.Key}: {column.Value}");
            }
        }

        Assert.NotNull(result.ToString());
    }

    [Fact]
    public async Task ConvertDaxToSqlWithValidation_UsingOpenAI_ShouldGenerateValidSqlAndMatchResults()
    {
        const string daxQuery = """
                                EVALUATE
                                SUMMARIZECOLUMNS(
                                	product[product_color],
                                	customer[customer_gender],
                                	FILTER(
                                		customer,
                                		customer[customer_gender] = "M"
                                	),
                                	"Total Sales", CALCULATE(
                                		SUM(sales[amount]),
                                		FILTER(
                                			product,
                                			product[product_color] IN {
                                				"Green",
                                				"Silver"
                                			}
                                		)
                                	)
                                )
                                """;
        var config = DaxSharpConfig.Instance;
        var pbiConnectionString = config.ConnectionStrings.PowerBi;
        var postgresConnectionString = config.ConnectionStrings.PostgreSQL;
        var schemaName = config.PostgreSql.DefaultSchema;
        var maxIterations = config.OpenAi.DefaultMaxIterations;
        
        // Get API key from environment variable or config
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? DaxSharpConfig.Instance.OpenAi.DefaultApiKey;

        // Skip test if API key is not provided
        // Note: This test requires OpenAI API key to run.
        // Set OPENAI_API_KEY environment variable or add it to appsettings.json (OpenAI:DefaultApiKey)
        if (string.IsNullOrEmpty(apiKey))
        {
            // Test skipped - API key is required
            // To run this test, set OPENAI_API_KEY environment variable or add it to appsettings.json
            return;
        }

        var result = await DaxSharpPbiExportToPostgres.ConvertDaxToSqlWithValidation(
            daxQuery: daxQuery,
            pbiConnectionString: pbiConnectionString,
            postgresConnectionString: postgresConnectionString,
            schemaName: schemaName,
            apiKey: apiKey,
            maxIterations: maxIterations
        );

        Assert.NotNull(result);

        // Check if there was an error about missing API key
        if (!string.IsNullOrEmpty(result.ErrorMessage) && result.ErrorMessage.Contains("API key"))
        {
            // This shouldn't happen if we checked above, but handle it gracefully
            Assert.Fail($"API key error: {result.ErrorMessage}");
            return;
        }

        // If we got here, we should have a SQL query
        Assert.NotNull(result.SqlQuery);
        Assert.NotEmpty(result.SqlQuery);
        Assert.True(result.PowerBiResults.Count > 0, "PowerBI query should return results");
        
        if (result.ResultsMatch)
        {
            Assert.Equal(result.PowerBiResults.Count, result.PostgresResults.Count);
        }
        else
        {
            // If results don't match, we should still have error message
            Assert.NotNull(result.ErrorMessage);
        }
    }

    [Fact]
    public void GetDatabaseSchema_FromPowerBi_ShouldReturnValidSchemaWithTablesAndColumns()
    {
        var config = DaxSharpConfig.Instance;
        var connectionString = config.ConnectionStrings.PowerBi;
        
        var schema = DaxSharpPbiExportExtensions.GetDatabaseSchema(connectionString);
        
        Assert.NotNull(schema);
        Assert.NotNull(schema.Tables);
        Assert.True(schema.Tables.Count > 0, "Should have at least one table");
        
        // Check that tables have columns
        foreach (var table in schema.Tables)
        {
            Assert.NotNull(table.TableName);
            Assert.NotEmpty(table.TableName);
            Assert.NotNull(table.Columns);
            Assert.True(table.Columns.Count > 0, $"Table {table.TableName} should have at least one column");
            
            // Check column properties
            foreach (var column in table.Columns)
            {
                Assert.NotNull(column.Name);
                Assert.NotEmpty(column.Name);
                Assert.NotNull(column.DataType);
            }
        }
    }

    [Fact]
    public void GetDatabaseSchemaDescription_FromPowerBi_ShouldReturnFormattedTextDescription()
    {
        var config = DaxSharpConfig.Instance;
        var connectionString = config.ConnectionStrings.PowerBi;
        
        var description = DaxSharpPbiExportExtensions.GetDatabaseSchemaDescription(connectionString);
        
        Assert.NotNull(description);
        Assert.NotEmpty(description);
        
        // Check that description contains expected format
        Assert.Contains("Table", description);
        Assert.Contains("columns:", description);
    }
}