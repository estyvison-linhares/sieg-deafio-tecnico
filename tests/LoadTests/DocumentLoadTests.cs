using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;
using NBomber.Contracts.Stats;

namespace LoadTests;

public class DocumentLoadTests
{
  private const string BaseUrl = "http://localhost:5000";
  private const string ApiBaseUrl = $"{BaseUrl}/api/documents";

  public static void Main(string[] args)
  {
    Console.WriteLine("🚀 Starting Load Tests for Fiscal Document API");
    Console.WriteLine($"📍 Target: {BaseUrl}");
    Console.WriteLine();

    var ingestScenario = CreateIngestScenario();
    var queryScenario = CreateQueryScenario();

    NBomberRunner
        .RegisterScenarios(ingestScenario, queryScenario)
        .WithReportFileName("fiscal_api_load_test")
        .WithReportFolder("Reports")
        .WithReportFormats(NBomber.Contracts.Stats.ReportFormat.Html, NBomber.Contracts.Stats.ReportFormat.Md)
        .Run();
  }

  private static ScenarioProps CreateIngestScenario()
  {
    var xmlContent = File.ReadAllBytes("Samples/nfe_sample.xml");
    var httpClient = new HttpClient();

    var scenario = Scenario.Create("ingest_xml", async context =>
    {
      using var content = new MultipartFormDataContent();
      var fileContent = new ByteArrayContent(xmlContent);
      fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");
      content.Add(fileContent, "xmlFile", "nfe_test.xml");

      var request = Http.CreateRequest("POST", $"{ApiBaseUrl}/upload")
              .WithBody(content);

      var response = await Http.Send(httpClient, request);

      return response;
    })
    .WithLoadSimulations(
        Simulation.Inject(
            rate: 10,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(30)
        )
    );

    return scenario;
  }

  private static ScenarioProps CreateQueryScenario()
  {
    var httpClient = new HttpClient();

    var scenario = Scenario.Create("query_documents", async context =>
    {
      var page = Random.Shared.Next(1, 5);
      var pageSize = 10;

      var request = Http.CreateRequest("GET", $"{ApiBaseUrl}?page={page}&pageSize={pageSize}");

      var response = await Http.Send(httpClient, request);

      return response;
    })
    .WithLoadSimulations(
        Simulation.Inject(
            rate: 50,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(30)
        )
    );

    return scenario;
  }
}
